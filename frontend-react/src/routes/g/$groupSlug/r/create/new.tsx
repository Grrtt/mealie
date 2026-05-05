import { useEffect, useState } from "react";
import Alert from "@mui/material/Alert";
import Button from "@mui/material/Button";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useNavigate, useParams } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { createRecipe } from "@/features/recipes/api";

export function RecipeCreateNewRouteComponent() {
  const { groupSlug } = useParams({ from: "/g/$groupSlug/r/create/new" });
  const navigate = useNavigate();
  const { data: user } = useCurrentUser();
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    document.title = "New Recipe · Mealie";
  }, []);

  return (
    <AppShell groupSlug={groupSlug} userName={user?.fullName} title="New Recipe">
      <Stack spacing={3}>
        <Typography variant="h4">Create a new recipe</Typography>
        {error ? <Alert severity="error">{error}</Alert> : null}
        <TextField label="Recipe name" value={name} onChange={event => setName(event.target.value)} />
        <Button
          variant="contained"
          disabled={!name.trim()}
          onClick={async () => {
            try {
              const recipe = await createRecipe(name.trim());
              if (!recipe.slug) throw new Error("Recipe slug missing from response");
              await navigate({ href: `/g/${groupSlug}/r/${recipe.slug}?edit=1` });
            }
            catch (createError) {
              setError(createError instanceof Error ? createError.message : "Unable to create recipe");
            }
          }}
        >
          Create recipe
        </Button>
      </Stack>
    </AppShell>
  );
}
