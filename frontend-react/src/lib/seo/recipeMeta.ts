import type { Recipe } from "@/lib/api/contracts";

function upsertMeta(selector: string, attr: "name" | "property", value: string, content?: string | null) {
  if (typeof document === "undefined") return;

  let meta = document.head.querySelector<HTMLMetaElement>(selector);
  if (!meta) {
    meta = document.createElement("meta");
    meta.setAttribute(attr, value);
    document.head.appendChild(meta);
  }

  if (content == null || content === "") {
    meta.removeAttribute("content");
    return;
  }

  meta.setAttribute("content", content);
}

export function applyRecipeMeta(recipe: Pick<Recipe, "name" | "description"> | null | undefined, options?: {
  prefix?: string;
}) {
  const title = recipe?.name
    ? options?.prefix
      ? `${recipe.name} · ${options.prefix}`
      : recipe.name
    : "Mealie";
  const description = recipe?.description?.trim() || "Recipe details in Mealie.";

  if (typeof document !== "undefined") {
    document.title = title;
  }

  upsertMeta('meta[name="description"]', "name", "description", description);
  upsertMeta('meta[property="og:title"]', "property", "og:title", title);
  upsertMeta('meta[property="og:description"]', "property", "og:description", description);
  upsertMeta('meta[property="twitter:title"]', "property", "twitter:title", title);
  upsertMeta('meta[property="twitter:description"]', "property", "twitter:description", description);
}
