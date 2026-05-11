import { useEffect } from "react";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { Recipe } from "@/lib/api/contracts";
import { recipeImageUrl } from "@/features/recipes/api";
import { formatRecipeDuration } from "@/features/recipes/format-duration";
import { applyRecipeMeta } from "@/lib/seo/recipeMeta";
import { RecipeImage } from "@/components/recipes/RecipeImage";

type Props = {
  recipe: Recipe;
};

export function PublicRecipePage({ recipe }: Props) {
  useEffect(() => {
    applyRecipeMeta(recipe, { prefix: "Mealie" });
  }, [recipe]);

  return (
    <Stack spacing={3}>
      <Card>
        <CardContent>
            <Stack spacing={3}>
              {recipeImageUrl(recipe.id, typeof recipe.image === "string" ? recipe.image : null) ? (
                <RecipeImage
                  alt={recipe.name ?? "Recipe image"}
                  src={recipeImageUrl(recipe.id, typeof recipe.image === "string" ? recipe.image : null)}
                  wrapperSx={{ minHeight: 280, maxHeight: 420, borderRadius: 2 }}
                />
              ) : null}
            <Stack spacing={1}>
              <Typography variant="h3">{recipe.name}</Typography>
              {recipe.description ? <Typography color="text.secondary">{recipe.description}</Typography> : null}
            </Stack>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              {recipe.recipeYield ? <Chip label={`Yield: ${recipe.recipeYield}`} /> : null}
              {recipe.totalTime ? <Chip label={`Total: ${formatRecipeDuration(recipe.totalTime)}`} /> : null}
              {(recipe.recipeCategory ?? []).map(category => <Chip key={category.id ?? category.slug} label={category.name} />)}
              {(recipe.tags ?? []).map(tag => <Chip key={tag.id ?? tag.slug} label={tag.name} />)}
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h5">Ingredients</Typography>
            <Stack component="ul" spacing={1} sx={{ pl: 3 }}>
              {(recipe.recipeIngredient ?? []).map((ingredient, index) => (
                <Typography key={`${ingredient.referenceId ?? ingredient.originalText ?? index}`} component="li">
                  {ingredient.originalText ?? ingredient.food?.name ?? "Ingredient"}
                </Typography>
              ))}
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h5">Instructions</Typography>
            <Stack component="ol" spacing={1} sx={{ pl: 3 }}>
              {(recipe.recipeInstructions ?? []).map((step, index) => (
                <Typography key={`${step.id ?? index}`} component="li">{step.text}</Typography>
              ))}
            </Stack>
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
