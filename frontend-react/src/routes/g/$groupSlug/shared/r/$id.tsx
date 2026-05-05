import { useEffect } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Container from "@mui/material/Container";
import { useParams } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { PublicRecipePage } from "@/components/recipes/PublicRecipePage";
import { resolveSharedRecipe } from "@/features/recipes/api";

export function SharedRecipeRouteComponent() {
  const { groupSlug, id } = useParams({ from: "/g/$groupSlug/shared/r/$id" });
  const recipeQuery = useQuery({
    queryKey: ["shared-recipe", groupSlug, id],
    queryFn: async () => await resolveSharedRecipe(groupSlug, id),
  });

  useEffect(() => {
    document.title = "Shared Recipe · Mealie";
  }, []);

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      {recipeQuery.isLoading ? (
        <Box sx={{ display: "grid", placeItems: "center", minHeight: "50vh" }}>
          <CircularProgress />
        </Box>
      ) : null}
      {recipeQuery.error ? (
        <Alert severity="error">
          {recipeQuery.error instanceof Error ? recipeQuery.error.message : "Unable to load shared recipe"}
        </Alert>
      ) : null}
      {recipeQuery.data ? <PublicRecipePage recipe={recipeQuery.data} /> : null}
    </Container>
  );
}
