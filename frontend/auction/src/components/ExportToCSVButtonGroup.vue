<template>
    <q-btn class="full-width" @click="exportToClipboard" icon="content_copy" unelevated color="primary" label="Export to clipboard">
      <q-tooltip class="bg-primary">Save auctions to clipboard</q-tooltip>
    </q-btn>
</template>

<script lang="ts" setup>
import { copyToClipboard, useQuasar } from 'quasar';

import { useRoomStore } from 'src/stores/RoomStore';

const roomStore = useRoomStore();
const { toCSV } = roomStore;

const $q = useQuasar();

async function exportToClipboard() {
  const exportString: string = await toCSV()
  if (exportString.length > 0) {
    copyToClipboard(exportString);
    $q.notify({
      color: 'warning',
      position: 'top',
      message: 'Exported to clipboard',
      icon: 'check',
    });
  } else {
    $q.notify({
      color: 'negative',
      position: 'top',
      message: 'No auctions to export',
      icon: 'report_problem',
    });
  }
  console.log({ exportString })
}
</script>
