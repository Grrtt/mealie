<template>
  <v-container fluid class="narrow-container">
    <BasePageTitle divider>
      <template #header>
        <v-icon size="80" color="primary">{{ $globals.icons.robot }}</v-icon>
      </template>
      <template #title>
        {{ $t("admin.ai-configurations") }}
      </template>
    </BasePageTitle>

    <!-- Add provider button -->
    <div class="d-flex justify-end mb-4">
      <BaseButton create @click="openCreateDialog">
        {{ $t("admin.add-ai-provider") }}
      </BaseButton>
    </div>

    <!-- Provider list -->
    <v-card v-if="configs.length">
      <template v-for="(config, idx) in configs" :key="config.id">
        <v-list-item :title="config.name" class="py-3">
          <template #prepend>
            <v-icon :color="config.isActive ? 'success' : 'grey'" class="opacity-100 mr-2">
              {{ config.isActive ? $globals.icons.checkboxMarkedCircle : $globals.icons.circle }}
            </v-icon>
          </template>
          <template #subtitle>
            <span class="text-caption">{{ config.providerType }}</span>
            <span v-if="config.defaultModel" class="text-caption ml-2">· {{ config.defaultModel }}</span>
            <span v-if="config.baseUrl" class="text-caption ml-2">· {{ config.baseUrl }}</span>
            <span v-if="config.maskedApiKey" class="text-caption ml-2 font-weight-medium">· {{ config.maskedApiKey }}</span>
          </template>
          <template #append>
            <div class="d-flex align-center" style="gap: 8px">
              <v-chip
                v-if="config.isActive"
                color="success"
                size="small"
                variant="tonal"
              >
                {{ $t("general.active") }}
              </v-chip>
              <BaseButton
                v-else
                size="small"
                color="info"
                secondary
                @click="activateConfig(config.id)"
              >
                {{ $t("admin.set-active") }}
              </BaseButton>
              <v-btn icon size="small" variant="text" @click="openEditDialog(config)">
                <v-icon>{{ $globals.icons.edit }}</v-icon>
              </v-btn>
              <v-btn icon size="small" variant="text" color="error" @click="confirmDelete(config)">
                <v-icon>{{ $globals.icons.delete }}</v-icon>
              </v-btn>
            </div>
          </template>
        </v-list-item>
        <v-divider v-if="idx < configs.length - 1" :key="`div-${config.id}`" />
      </template>
    </v-card>
    <v-card v-else class="text-center pa-8 text-medium-emphasis">
      {{ $t("admin.no-ai-providers") }}
    </v-card>

    <!-- Create / Edit dialog -->
    <BaseDialog
      v-model="dialog.show"
      :title="dialog.editing ? $t('admin.edit-ai-provider') : $t('admin.add-ai-provider')"
      :icon="$globals.icons.robot"
      :can-confirm="true"
      @confirm="saveConfig"
    >
      <v-card-text class="pt-4">
        <v-text-field
          v-model="form.name"
          :label="$t('general.name')"
          variant="outlined"
          class="mb-3"
          required
        />
        <v-select
          v-model="form.providerType"
          :label="$t('admin.provider-type')"
          :items="providerTypes"
          item-title="text"
          item-value="value"
          variant="outlined"
          class="mb-3"
        />
        <v-text-field
          v-model="form.apiKey"
          :label="dialog.editing ? $t('admin.api-key-leave-blank') : $t('admin.api-key')"
          variant="outlined"
          type="password"
          autocomplete="new-password"
          class="mb-3"
        />
        <v-text-field
          v-model="form.baseUrl"
          :label="$t('admin.base-url-optional')"
          variant="outlined"
          class="mb-3"
        />
        <v-text-field
          v-model="form.defaultModel"
          :label="$t('admin.default-model')"
          variant="outlined"
          class="mb-3"
        />
        <v-checkbox
          v-model="form.enableImageServices"
          :label="$t('admin.enable-image-services')"
          color="primary"
          hide-details
        />
        <v-checkbox
          v-model="form.enableTranscriptionServices"
          :label="$t('admin.enable-transcription-services')"
          color="primary"
          hide-details
        />
      </v-card-text>
    </BaseDialog>

    <!-- Delete confirmation -->
    <BaseDialog
      v-model="deleteDialog.show"
      :title="$t('admin.delete-ai-provider')"
      :icon="$globals.icons.alertCircle"
      color="error"
      :can-confirm="true"
      @confirm="deleteConfig"
    >
      <v-card-text>
        {{ $t("admin.delete-ai-provider-confirm", { name: deleteDialog.target?.name }) }}
      </v-card-text>
    </BaseDialog>
  </v-container>
</template>

<script setup lang="ts">
import { useAdminApi } from "~/composables/api";
import { alert } from "~/composables/use-toast";
import { useGlobalI18n } from "~/composables/use-global-i18n";
import type { AiConfigurationResponse } from "~/lib/api/admin/admin-ai-configurations";

definePageMeta({ layout: "admin" });

onMounted(() => { setPageLayout("admin"); });

const { $globals } = useNuxtApp();
const i18n = useGlobalI18n();
const adminApi = useAdminApi();

useSeoMeta({ title: i18n.t("admin.ai-configurations") });

const configs = ref<AiConfigurationResponse[]>([]);
async function loadConfigs() {
  const { data } = await adminApi.aiConfigurations.getAll();
  if (data) configs.value = data;
}

onMounted(loadConfigs);

const providerTypes = [
  { text: "OpenAI", value: "openAi" },
  { text: "Azure OpenAI", value: "azureOpenAi" },
  { text: "Anthropic", value: "anthropic" },
  { text: "Ollama", value: "ollama" },
  { text: "Custom (OpenAI-compatible)", value: "custom" },
];

const defaultForm = () => ({
  name: "",
  providerType: "openAi",
  apiKey: "",
  baseUrl: "",
  defaultModel: "",
  enableImageServices: true,
  enableTranscriptionServices: true,
});

const form = reactive(defaultForm());

const dialog = reactive({
  show: false,
  editing: false,
  editingId: "" as string,
});

function openCreateDialog() {
  Object.assign(form, defaultForm());
  dialog.editing = false;
  dialog.editingId = "";
  dialog.show = true;
}

function openEditDialog(config: AiConfigurationResponse) {
  form.name = config.name;
  form.providerType = config.providerType;
  form.apiKey = "";
  form.baseUrl = config.baseUrl ?? "";
  form.defaultModel = config.defaultModel ?? "";
  form.enableImageServices = config.enableImageServices;
  form.enableTranscriptionServices = config.enableTranscriptionServices;
  dialog.editing = true;
  dialog.editingId = config.id;
  dialog.show = true;
}

async function saveConfig() {
  if (dialog.editing) {
    const { error } = await adminApi.aiConfigurations.update(dialog.editingId, {
      name: form.name || null,
      apiKey: form.apiKey || null,
      baseUrl: form.baseUrl || null,
      defaultModel: form.defaultModel || null,
      enableImageServices: form.enableImageServices,
      enableTranscriptionServices: form.enableTranscriptionServices,
    });
    if (error) { alert.error(i18n.t("events.something-went-wrong")); return; }
    alert.success(i18n.t("general.item-updated"));
  }
  else {
    const { error } = await adminApi.aiConfigurations.create({
      name: form.name,
      providerType: form.providerType,
      apiKey: form.apiKey || null,
      baseUrl: form.baseUrl || null,
      defaultModel: form.defaultModel || null,
      enableImageServices: form.enableImageServices,
      enableTranscriptionServices: form.enableTranscriptionServices,
    });
    if (error) { alert.error(i18n.t("events.something-went-wrong")); return; }
    alert.success(i18n.t("general.item-created"));
  }
  dialog.show = false;
  await loadConfigs();
}

async function activateConfig(id: string) {
  const { error } = await adminApi.aiConfigurations.activate(id);
  if (error) { alert.error(i18n.t("events.something-went-wrong")); return; }
  await loadConfigs();
}

const deleteDialog = reactive({
  show: false,
  target: null as AiConfigurationResponse | null,
});

function confirmDelete(config: AiConfigurationResponse) {
  deleteDialog.target = config;
  deleteDialog.show = true;
}

async function deleteConfig() {
  if (!deleteDialog.target) return;
  const { error } = await adminApi.aiConfigurations.delete(deleteDialog.target.id);
  if (error) { alert.error(i18n.t("events.something-went-wrong")); return; }
  alert.success(i18n.t("general.item-deleted"));
  deleteDialog.show = false;
  await loadConfigs();
}
</script>
