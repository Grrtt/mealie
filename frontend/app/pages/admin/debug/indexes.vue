<template>
  <v-container class="pa-0">
    <v-container>
      <BaseCardSectionTitle title="Search Indexes">
        Manage Lucene.NET search indexes. View document counts, rebuild indexes from the database, delete index data, or run raw diagnostic searches.
      </BaseCardSectionTitle>

      <!-- Index cards -->
      <v-row>
        <v-col
          v-for="index in indexes"
          :key="index.name"
          cols="12"
          md="6"
        >
          <v-card>
            <v-card-title class="text-capitalize">
              {{ index.name }}
            </v-card-title>
            <v-card-text>
              <div class="d-flex flex-wrap gap-3">
                <v-chip prepend-icon="$info" variant="outlined">
                  {{ index.documentCount.toLocaleString() }} documents
                </v-chip>
                <v-chip prepend-icon="$storage" variant="outlined">
                  {{ formatBytes(index.directorySizeBytes) }}
                </v-chip>
              </div>
              <div class="text-caption text-medium-emphasis mt-2">
                {{ index.directoryPath }}
              </div>
            </v-card-text>
            <v-card-actions>
              <v-btn
                color="primary"
                variant="tonal"
                :loading="rebuilding === index.name"
                prepend-icon="$refresh"
                @click="rebuildIndex(index.name)"
              >
                Rebuild
              </v-btn>
              <v-btn
                color="error"
                variant="tonal"
                :loading="deleting === index.name"
                prepend-icon="$delete"
                @click="confirmDelete(index.name)"
              >
                Delete
              </v-btn>
            </v-card-actions>
          </v-card>
        </v-col>
      </v-row>

      <!-- Raw search section -->
      <v-card class="mt-6">
        <v-card-title>Raw Search</v-card-title>
        <v-card-text>
          <v-row>
            <v-col cols="12" sm="4">
              <v-select
                v-model="searchState.indexName"
                :items="indexNames"
                label="Index"
                density="compact"
                hide-details
              />
            </v-col>
            <v-col cols="12" sm="5">
              <v-text-field
                v-model="searchState.query"
                label="Query (empty = match all)"
                density="compact"
                hide-details
                clearable
                @keyup.enter="runSearch"
              />
            </v-col>
            <v-col cols="12" sm="2">
              <v-text-field
                v-model.number="searchState.maxResults"
                label="Max results"
                type="number"
                density="compact"
                hide-details
                min="1"
                max="500"
              />
            </v-col>
            <v-col cols="12" sm="1" class="d-flex align-center">
              <v-btn
                color="primary"
                :loading="searchState.loading"
                icon
                @click="runSearch"
              >
                <v-icon>{{ $globals.icons.search }}</v-icon>
              </v-btn>
            </v-col>
          </v-row>
        </v-card-text>

        <template v-if="searchResult">
          <v-divider />
          <v-card-text>
            <div class="text-body-2 mb-2">
              {{ searchResult.totalHits }} total hits for <em>{{ searchResult.indexName }}</em>
            </div>
            <v-data-table
              :headers="searchHeaders"
              :items="searchResult.documents"
              density="compact"
              class="elevation-0"
            />
          </v-card-text>
        </template>
      </v-card>
    </v-container>

    <!-- Delete confirmation dialog -->
    <v-dialog v-model="deleteDialog.open" max-width="480">
      <v-card>
        <v-card-title>Delete Index</v-card-title>
        <v-card-text>
          Are you sure you want to delete the <strong>{{ deleteDialog.name }}</strong> index? It will be rebuilt automatically on the next search or you can trigger a manual rebuild.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="deleteDialog.open = false">Cancel</v-btn>
          <v-btn
            color="error"
            variant="flat"
            :loading="deleting === deleteDialog.name"
            @click="deleteIndex(deleteDialog.name)"
          >
            Delete
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Snackbar feedback -->
    <v-snackbar v-model="snackbar.open" :color="snackbar.color" timeout="3000">
      {{ snackbar.message }}
    </v-snackbar>
  </v-container>
</template>

<script setup lang="ts">
import { useAdminApi } from "~/composables/api";
import type { IndexInfo, IndexSearchResponse } from "~/lib/api/types/admin";

definePageMeta({ layout: "admin" });

useSeoMeta({ title: "Search Indexes" });

const api = useAdminApi();

// ── Index list ─────────────────────────────────────────────────────────────

const indexes = ref<IndexInfo[]>([]);
const rebuilding = ref<string | null>(null);
const deleting = ref<string | null>(null);

async function loadIndexes() {
  const { data } = await api.indexes.getAll();
  if (data) indexes.value = data;
}

async function rebuildIndex(name: string) {
  rebuilding.value = name;
  try {
    const { data, error } = await api.indexes.rebuild(name);
    if (error) {
      showSnackbar("Rebuild failed", "error");
    }
    else {
      showSnackbar(data?.detail ?? "Rebuild started", "success");
      await loadIndexes();
    }
  }
  finally {
    rebuilding.value = null;
  }
}

const deleteDialog = reactive({ open: false, name: "" });

function confirmDelete(name: string) {
  deleteDialog.name = name;
  deleteDialog.open = true;
}

async function deleteIndex(name: string) {
  deleting.value = name;
  try {
    const { error } = await api.indexes.delete(name);
    deleteDialog.open = false;
    if (error) {
      showSnackbar("Delete failed", "error");
    }
    else {
      showSnackbar(`Index '${name}' deleted`, "success");
      await loadIndexes();
    }
  }
  finally {
    deleting.value = null;
  }
}

// ── Raw search ─────────────────────────────────────────────────────────────

const indexNames = computed(() => indexes.value.map(i => i.name));

const searchState = reactive({
  indexName: "",
  query: "",
  maxResults: 50,
  loading: false,
});

const searchResult = ref<IndexSearchResponse | null>(null);

const searchHeaders = computed(() => {
  if (!searchResult.value?.documents.length) return [];
  return Object.keys(searchResult.value.documents[0]).map(key => ({
    title: key,
    key,
    sortable: false,
  }));
});

async function runSearch() {
  if (!searchState.indexName) return;
  searchState.loading = true;
  try {
    const { data, error } = await api.indexes.search(searchState.indexName, {
      query: searchState.query || null,
      maxResults: searchState.maxResults,
    });
    if (error) {
      showSnackbar("Search failed", "error");
    }
    else {
      searchResult.value = data ?? null;
    }
  }
  finally {
    searchState.loading = false;
  }
}

// ── Snackbar ────────────────────────────────────────────────────────────────

const snackbar = reactive({ open: false, message: "", color: "success" });

function showSnackbar(message: string, color: "success" | "error") {
  snackbar.message = message;
  snackbar.color = color;
  snackbar.open = true;
}

// ── Utilities ───────────────────────────────────────────────────────────────

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

// ── Init ────────────────────────────────────────────────────────────────────

onMounted(async () => {
  await loadIndexes();
  if (indexes.value.length) {
    searchState.indexName = indexes.value[0].name;
  }
});
</script>
