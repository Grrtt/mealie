<template>
  <v-container fluid>
    <!-- Create Alias Dialog -->
    <v-dialog
      v-model="createDialog.open"
      max-width="500"
      @after-leave="resetCreateDialog"
    >
      <v-card>
        <v-card-title class="d-flex align-center">
          <v-icon
            start
            :icon="$globals.icons.tagArrowRight"
          />
          Create Ingredient Alias
        </v-card-title>
        <v-card-text>
          <v-text-field
            :model-value="createDialog.rawText"
            label="Ingredient Text"
            readonly
            variant="outlined"
            density="compact"
            class="mb-4"
          />
          <v-autocomplete
            v-model="createDialog.selectedFood"
            v-model:search="createDialog.foodSearch"
            :items="foodSearchResults"
            item-title="name"
            item-value="id"
            label="Map to Food"
            variant="outlined"
            density="compact"
            :loading="createDialog.foodLoading"
            no-filter
            return-object
            clearable
            placeholder="Search foods…"
            class="mb-2"
          />
          <v-checkbox
            v-model="createDialog.backfillRecipes"
            label="Also update existing recipes"
            density="compact"
            hide-details
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn
            variant="text"
            @click="createDialog.open = false"
          >
            Cancel
          </v-btn>
          <v-btn
            color="primary"
            variant="elevated"
            :disabled="!createDialog.selectedFood"
            :loading="createDialog.submitting"
            @click="submitCreateAlias"
          >
            Create Alias
          </v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <!-- Delete Confirmation Dialog -->
    <BaseDialog
      v-model="deleteDialog.open"
      title="Confirm"
      :icon="$globals.icons.alertCircle"
      color="error"
      can-confirm
      @confirm="confirmDelete"
    >
      <template #activator />
      <v-card-text>
        Are you sure you want to delete the alias <strong>{{ deleteDialog.aliasName }}</strong>?
      </v-card-text>
    </BaseDialog>

    <BaseCardSectionTitle title="Ingredient Aliases" />

    <v-tabs
      v-model="activeTab"
      color="primary"
      class="mb-4"
    >
      <v-tab value="unresolved">
        Unresolved
        <v-chip
          v-if="unresolved.total > 0"
          class="ml-2"
          size="small"
          color="warning"
        >
          {{ unresolved.total }}
        </v-chip>
      </v-tab>
      <v-tab value="existing">
        Existing Aliases
        <v-chip
          v-if="existing.total > 0"
          class="ml-2"
          size="small"
        >
          {{ existing.total }}
        </v-chip>
      </v-tab>
    </v-tabs>

    <v-window v-model="activeTab">
      <!-- Tab 1: Unresolved -->
      <v-window-item value="unresolved">
        <v-toolbar
          flat
          color="transparent"
        >
          <v-text-field
            v-model="unresolved.search"
            :prepend-inner-icon="$globals.icons.search"
            label="Search ingredients…"
            variant="outlined"
            density="compact"
            clearable
            hide-details
            class="mr-4"
            style="max-width: 400px"
            @update:model-value="onUnresolvedSearchChange"
          />
        </v-toolbar>

        <v-data-table
          :headers="unresolvedHeaders"
          :items="unresolved.items"
          :loading="unresolved.loading"
          item-key="rawText"
          class="elevation-0"
          :items-per-page="-1"
          hide-default-footer
          disable-pagination
        >
          <template #[`item.count`]="{ item }">
            <v-chip
              size="small"
              color="info"
            >
              {{ item.count }}
            </v-chip>
          </template>
          <template #[`item.actions`]="{ item }">
            <v-btn
              size="small"
              color="primary"
              variant="tonal"
              @click="openCreateDialog(item.rawText)"
            >
              Create Alias
            </v-btn>
          </template>
        </v-data-table>

        <div
          v-if="unresolved.totalPages > 1"
          class="d-flex justify-center mt-4"
        >
          <v-pagination
            v-model="unresolved.page"
            :length="unresolved.totalPages"
            @update:model-value="fetchUnresolved"
          />
        </div>
      </v-window-item>

      <!-- Tab 2: Existing Aliases -->
      <v-window-item value="existing">
        <v-data-table
          :headers="existingHeaders"
          :items="existing.items"
          :loading="existing.loading"
          item-key="id"
          class="elevation-0"
          :items-per-page="-1"
          hide-default-footer
          disable-pagination
        >
          <template #[`item.arrow`]>
            <v-icon size="small">
              {{ $globals.icons.arrowRight }}
            </v-icon>
          </template>
          <template #[`item.actions`]="{ item }">
            <v-btn
              icon
              color="error"
              variant="text"
              @click="openDeleteDialog(item)"
            >
              <v-icon>{{ $globals.icons.delete }}</v-icon>
            </v-btn>
          </template>
        </v-data-table>

        <div
          v-if="existing.totalPages > 1"
          class="d-flex justify-center mt-4"
        >
          <v-pagination
            v-model="existing.page"
            :length="existing.totalPages"
            @update:model-value="fetchExisting"
          />
        </div>
      </v-window-item>
    </v-window>
  </v-container>
</template>

<script setup lang="ts">
import { useAdminApi, useUserApi } from "~/composables/api";
import { alert } from "~/composables/use-toast";
import type { UnresolvedIngredient, IngredientAlias } from "~/lib/api/admin/admin-ingredient-aliases";
import type { IngredientFood } from "~/lib/api/types/recipe";

definePageMeta({
  layout: "admin",
});

const { $globals } = useNuxtApp();
const adminApi = useAdminApi();
const userApi = useUserApi();

// ===== Tab State =====
const activeTab = ref<"unresolved" | "existing">("unresolved");

watch(activeTab, (tab) => {
  if (tab === "existing") {
    fetchExisting();
  }
});

// ===== Unresolved Tab =====
const unresolved = reactive({
  items: [] as UnresolvedIngredient[],
  loading: false,
  page: 1,
  perPage: 50,
  total: 0,
  totalPages: 0,
  search: "",
});

const unresolvedHeaders = [
  { title: "Ingredient Text", value: "rawText", sortable: false },
  { title: "Recipe Count", value: "count", sortable: false, align: "center" as const },
  { title: "Action", value: "actions", sortable: false, align: "end" as const },
];

let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

function onUnresolvedSearchChange() {
  if (searchDebounceTimer) clearTimeout(searchDebounceTimer);
  searchDebounceTimer = setTimeout(() => {
    unresolved.page = 1;
    fetchUnresolved();
  }, 400);
}

async function fetchUnresolved() {
  unresolved.loading = true;
  try {
    const { data } = await adminApi.ingredientAliases.getUnresolved(
      unresolved.page,
      unresolved.perPage,
      unresolved.search || undefined,
    );
    if (data) {
      unresolved.items = data.items ?? [];
      unresolved.total = data.total ?? 0;
      unresolved.totalPages = data.total_pages ?? 0;
    }
  } catch {
    alert.error("Failed to load unresolved ingredients");
  } finally {
    unresolved.loading = false;
  }
}

// ===== Existing Aliases Tab =====
const existing = reactive({
  items: [] as IngredientAlias[],
  loading: false,
  page: 1,
  perPage: 50,
  total: 0,
  totalPages: 0,
});

const existingHeaders = [
  { title: "Alias Text", value: "name", sortable: false },
  { title: "", value: "arrow", sortable: false, align: "center" as const, width: 40 },
  { title: "Food", value: "foodName", sortable: false },
  { title: "Delete", value: "actions", sortable: false, align: "end" as const },
];

async function fetchExisting() {
  existing.loading = true;
  try {
    const { data } = await adminApi.ingredientAliases.getAll(existing.page, existing.perPage);
    if (data) {
      existing.items = data.items ?? [];
      existing.total = data.total ?? 0;
      existing.totalPages = data.total_pages ?? 0;
    }
  } catch {
    alert.error("Failed to load ingredient aliases");
  } finally {
    existing.loading = false;
  }
}

// ===== Create Alias Dialog =====
const createDialog = reactive({
  open: false,
  rawText: "",
  selectedFood: null as IngredientFood | null,
  foodSearch: "",
  foodLoading: false,
  backfillRecipes: true,
  submitting: false,
});

const foodSearchResults = ref<IngredientFood[]>([]);

let foodSearchDebounce: ReturnType<typeof setTimeout> | null = null;

watch(
  () => createDialog.foodSearch,
  (val) => {
    if (foodSearchDebounce) clearTimeout(foodSearchDebounce);
    if (!val || val.length < 1) {
      foodSearchResults.value = [];
      return;
    }
    foodSearchDebounce = setTimeout(() => searchFoods(val), 300);
  },
);

async function searchFoods(query: string) {
  createDialog.foodLoading = true;
  try {
    const { data } = await userApi.foods.getAll(1, 25, { search: query });
    foodSearchResults.value = data?.items ?? [];
  } catch {
    // ignore search errors
  } finally {
    createDialog.foodLoading = false;
  }
}

function openCreateDialog(rawText: string) {
  createDialog.rawText = rawText;
  createDialog.selectedFood = null;
  createDialog.foodSearch = "";
  createDialog.backfillRecipes = true;
  createDialog.open = true;
  foodSearchResults.value = [];
}

function resetCreateDialog() {
  createDialog.rawText = "";
  createDialog.selectedFood = null;
  createDialog.foodSearch = "";
  createDialog.submitting = false;
}

async function submitCreateAlias() {
  if (!createDialog.selectedFood) return;
  createDialog.submitting = true;
  try {
    const { data, error } = await adminApi.ingredientAliases.create(
      createDialog.rawText,
      createDialog.selectedFood.id,
      createDialog.backfillRecipes,
    );
    if (error) {
      alert.error("Failed to create alias");
      return;
    }
    if (data) {
      alert.success(`Alias created: "${data.name}" → ${data.foodName}`);
      createDialog.open = false;
      unresolved.page = 1;
      fetchUnresolved();
    }
  } finally {
    createDialog.submitting = false;
  }
}

// ===== Delete Dialog =====
const deleteDialog = reactive({
  open: false,
  aliasId: "",
  aliasName: "",
});

function openDeleteDialog(alias: IngredientAlias) {
  deleteDialog.aliasId = alias.id;
  deleteDialog.aliasName = alias.name;
  deleteDialog.open = true;
}

async function confirmDelete() {
  if (!deleteDialog.aliasId) return;
  try {
    await adminApi.ingredientAliases.delete(deleteDialog.aliasId);
    alert.success("Alias deleted");
    fetchExisting();
  } catch {
    alert.error("Failed to delete alias");
  } finally {
    deleteDialog.open = false;
    deleteDialog.aliasId = "";
    deleteDialog.aliasName = "";
  }
}

// ===== Init =====
onMounted(() => {
  fetchUnresolved();
});

useSeoMeta({ title: "Ingredient Aliases" });
useHead({ title: "Ingredient Aliases" });
</script>
