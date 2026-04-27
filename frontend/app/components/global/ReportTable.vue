<template>
  <v-data-table
    :headers="headers"
    :items="items"
    item-key="id"
    class="elevation-0"
    :items-per-page="50"
    @click:row="($event, { item }) => handleRowClick(item)"
  >
    <template #[`item.category`]="{ item }">
      {{ capitalize(item.category) }}
    </template>
    <template #[`item.timestamp`]="{ item }">
      {{ $d(Date.parse(item.timestamp!), "long") }}
    </template>
    <template #[`item.status`]="{ item }">
      {{ capitalize(item.status!) }}
    </template>
    <template #[`item.progress`]="{ item }">
      <div
        v-if="item.status === 'queued'"
        class="text-medium-emphasis"
      >
        {{ $t('general.queued') }}
      </div>
      <div
        v-else-if="item.status === 'in-progress'"
        style="min-width: 140px;"
      >
        <div class="d-flex justify-space-between text-caption mb-1">
          <span>{{ item.processedCount ?? 0 }} / {{ item.totalCount ?? '?' }}</span>
        </div>
        <v-progress-linear
          :model-value="item.totalCount ? ((item.processedCount ?? 0) / item.totalCount) * 100 : null"
          :indeterminate="!item.totalCount"
          color="primary"
          rounded
          height="6"
        />
      </div>
      <span v-else-if="item.totalCount != null">
        {{ item.processedCount }} / {{ item.totalCount }}
      </span>
    </template>
    <template #[`item.actions`]="{ item }">
      <v-btn
        icon
        @click.stop="deleteReport(item.id)"
      >
        <v-icon>{{ $globals.icons.delete }}</v-icon>
      </v-btn>
    </template>
  </v-data-table>
</template>

<script setup lang="ts">
import type { ReportSummary } from "~/lib/api/types/reports";

defineProps({
  items: {
    type: Array as () => Array<ReportSummary>,
    required: true,
  },
});

const emit = defineEmits<{
  (e: "delete", id: string): void;
}>();

const i18n = useI18n();
const router = useRouter();

const headers = [
  { title: i18n.t("category.category"), value: "category", key: "category" },
  { title: i18n.t("general.name"), value: "name", key: "name" },
  { title: i18n.t("general.timestamp"), value: "timestamp", key: "timestamp" },
  { title: i18n.t("general.status"), value: "status", key: "status" },
  { title: i18n.t("general.progress"), value: "progress", key: "progress", sortable: false },
  { title: i18n.t("general.delete"), value: "actions", key: "actions" },
];

function handleRowClick(item: ReportSummary) {
  if (item.status === "in-progress" || item.status === "queued") {
    return;
  }

  router.push(`/group/reports/${item.id}`);
}

function capitalize(str: string) {
  return str.charAt(0).toUpperCase() + str.slice(1);
}

function deleteReport(id: string) {
  emit("delete", id);
}
</script>

<style lang="scss" scoped></style>
