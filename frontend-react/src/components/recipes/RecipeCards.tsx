import Card from "@mui/material/Card";
import CardActionArea from "@mui/material/CardActionArea";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Grid from "@mui/material/Grid";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";
import type { RecipeSummary } from "@/lib/api/contracts";
import { recipeImageUrl } from "@/features/recipes/api";
import { formatRecipeDuration } from "@/features/recipes/format-duration";
import { apiClient } from "@/lib/api/client";
import { RecipeImage } from "@/components/recipes/RecipeImage";

type Props = {
  recipes: RecipeSummary[];
  hrefBuilder: (recipe: RecipeSummary) => string;
};

export function RecipeCards({ recipes, hrefBuilder }: Props) {
  return (
    <Grid container spacing={2}>
      {recipes.map(recipe => (
        <Grid key={recipe.id ?? recipe.slug} size={{ xs: 12, md: 6, lg: 4 }}>
          <Card sx={{ height: "100%" }}>
              <CardActionArea href={apiClient.resolvePath(hrefBuilder(recipe))} sx={{ height: "100%" }}>
                {recipe.id ? (
                  <RecipeImage
                    alt={recipe.name ?? "Recipe image"}
                    src={recipeImageUrl(recipe.id, typeof recipe.image === "string" ? recipe.image : null, "min-original.webp")}
                    wrapperSx={{ height: 180 }}
                  />
                ) : null}
                <CardContent>
                <Stack spacing={1}>
                  <Typography variant="h6">{recipe.name ?? "Untitled recipe"}</Typography>
                  {recipe.description ? (
                    <Typography variant="body2" color="text.secondary">
                      {recipe.description}
                    </Typography>
                  ) : null}
                  <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                    {recipe.totalTime ? <Chip size="small" label={formatRecipeDuration(recipe.totalTime)} /> : null}
                    {recipe.rating ? <Chip size="small" label={`★ ${recipe.rating}`} /> : null}
                  </Stack>
                </Stack>
              </CardContent>
            </CardActionArea>
          </Card>
        </Grid>
      ))}
    </Grid>
  );
}
