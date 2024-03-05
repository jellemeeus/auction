using AuctionApi.Models;
using AuctionApi.Services;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace AuctionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(RoomsService roomsService, WarcraftService warcraftService) : ControllerBase
{
    private readonly RoomsService _roomsService = roomsService;
    private readonly WarcraftService _itemsService = warcraftService;

    [HttpGet]
    public async Task<List<Room>> Get() =>
        await _roomsService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Room>> Get(string id)
    {
        var room = await _roomsService.GetAsync(id);

        if (room is null)
        {
            return NotFound();
        }

        return room;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Post(string Namespace)
    {
        // Validate if Namespace matches a valid namespace
        if (!(new[]{
             APINamespace.Era, APINamespace.Progression, APINamespace.Retail
            }.Contains(Namespace)))
        {
            return BadRequest("Namespace must match a valid namespace. ");
        };

        Room newRoom = new()
        {
            Namespace = Namespace
        };
        await _roomsService.CreateAsync(newRoom);

        return CreatedAtAction(nameof(Get), new { id = newRoom.Id }, newRoom);
    }

    [HttpPost]
    public async Task<IActionResult> Post(Room newRoom)
    {
        await _roomsService.CreateAsync(newRoom);

        return CreatedAtAction(nameof(Get), new { id = newRoom.Id }, newRoom);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Room updatedRoom)
    {
        var room = await _roomsService.GetAsync(id);

        if (room is null)
        {
            return NotFound();
        }

        updatedRoom.Id = room.Id;

        await _roomsService.UpdateAsync(id, updatedRoom);

        return NoContent();
    }

    [HttpPut("{id:length(24)}/items")]
    public async Task<IActionResult> UpdateItems(string id, List<Auction> newAuctions)
    {
        Room? room = await _roomsService.GetAsync(id);

        if (room is null)
        {
            return NotFound();
        }

        // Set default values
        int i = 1;
        foreach (Auction auction in newAuctions)
        {
            // Set fields from room settings
            auction.MinimumPrice = room.MinimumBid;
            auction.Status = Status.Pending;
            auction.RowId = i++;
            // Update item info from warcraft API
            LocalItemInfo item = await _itemsService.GetFromWarcraftAPI(auction.ItemId, room.Namespace);
            auction.Quality = item.Quality;
            auction.ItemLevel = item.Level;
            auction.ItemName = item.Name;
        }

        room.Auctions = newAuctions;

        await _roomsService.UpdateAsync(id, room);

        return NoContent();
    }


    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var auction = await _roomsService.GetAsync(id);

        if (auction is null)
        {
            return NotFound();
        }

        await _roomsService.RemoveAsync(id);

        return NoContent();
    }


    [HttpPatch("{id:length(24)}/start")]
    public async Task<ActionResult<Room>> Patch(string id)
    {
        Room? room = await _roomsService.GetAsync(id);

        if (room is null || room.Auctions is null)
        {
            return NotFound();
        }

        // Set pending auctions to bid
        // Set expiration date
        long StartTimeUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        foreach (Auction auction in room.Auctions)
        {
            if (auction.Status == Status.Pending)
            {
                auction.Status = Status.Bidding;
                auction.Expiration = StartTimeUnixTimestamp + room.BidDurationInSeconds;
            }
        }

        await _roomsService.UpdateAsync(id, room);

        return NoContent();
    }

    /// Updates the bid for the auction with the given row ID and item ID.
    /// Validates that the auction exists and is in progress.
    /// Ensures the bid meets the minimum bid amount based on current bid or starting price.
    [HttpPatch("{id:length(24)}")]
    public async Task<ActionResult<Room>> Patch(string id, BidRequest newBid)
    {
        Room? room = await _roomsService.GetAsync(id);

        if (room is null || room.Auctions is null)
        {
            return NotFound();
        }

        // RowId and ItemId must match the auction we're trying to update
        Auction? auction = room.Auctions.Where(a => (a.RowId == newBid.RowId) && (a.ItemId == newBid.ItemId)).FirstOrDefault();
        if (auction is null)
        {
            return NotFound();
        }

        int BidMinimumAcceptable;
        bool NoBidHasBeenPlaced = auction.Bid is null || auction.BidderName is null;
        if (NoBidHasBeenPlaced)
        {
            BidMinimumAcceptable = auction.MinimumPrice ?? room.MinimumBid;
        }
        else
        {
            BidMinimumAcceptable = (int)auction.Bid! + room.MinimumBidIncrement;
        }

        if (newBid.MyBid < BidMinimumAcceptable)
        {
            return BadRequest($"Bid must be at least {BidMinimumAcceptable}");
        }

        // TODO: add updatedAuction.Status == 2 to verify auction is in progress

        auction.Bid = newBid.MyBid;
        auction.BidderName = newBid.MyName;

        await _roomsService.UpdateAsync(id, room);

        return NoContent();
    }
}