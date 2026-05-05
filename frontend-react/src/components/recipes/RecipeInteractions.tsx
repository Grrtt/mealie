import { useEffect, useMemo, useState } from "react";
import Alert from "@mui/material/Alert";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Chip from "@mui/material/Chip";
import Divider from "@mui/material/Divider";
import Rating from "@mui/material/Rating";
import Stack from "@mui/material/Stack";
import Switch from "@mui/material/Switch";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation, useQuery } from "@tanstack/react-query";
import type { PrivateUser, Recipe } from "@/lib/api/contracts";
import {
  addRecipeComment,
  createRecipeShareToken,
  fetchRecipeComments,
  fetchRecipeShareTokens,
  fetchSelfRatings,
  setRecipeRating,
  updateLastMade,
  uploadRecipeAsset,
  uploadRecipeImage,
} from "@/features/recipes/api";
import { apiClient } from "@/lib/api/client";

type Props = {
  groupSlug: string;
  recipe: Recipe;
  currentUser: PrivateUser | null;
  onRecipeRefresh: () => Promise<void> | void;
};

function buildShareUrl(groupSlug: string, tokenId: string) {
  return new URL(apiClient.resolvePath(`/g/${groupSlug}/shared/r/${tokenId}`), window.location.origin).toString();
}

export function RecipeInteractions({ groupSlug, recipe, currentUser, onRecipeRefresh }: Props) {
  const [commentText, setCommentText] = useState("");
  const [ratingValue, setRatingValue] = useState<number | null>(recipe.rating ?? null);
  const [favorite, setFavorite] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const commentsQuery = useQuery({
    queryKey: ["recipe-comments", recipe.slug],
    queryFn: async () => {
      if (!recipe.slug) return [];
      return await fetchRecipeComments(recipe.slug);
    },
    enabled: Boolean(recipe.slug) && !recipe.settings?.disableComments,
  });

  const sharesQuery = useQuery({
    queryKey: ["recipe-shares", recipe.slug],
    queryFn: async () => {
      if (!recipe.slug) return [];
      return await fetchRecipeShareTokens(recipe.slug);
    },
    enabled: Boolean(recipe.slug),
  });

  const ratingsQuery = useQuery({
    queryKey: ["self-ratings"],
    queryFn: fetchSelfRatings,
    enabled: Boolean(currentUser?.id && recipe.id),
  });

  useEffect(() => {
    const existing = ratingsQuery.data?.find(entry => entry.recipeId === recipe.id);
    setRatingValue(existing?.rating ?? recipe.rating ?? null);
    setFavorite(Boolean(existing?.isFavorite));
  }, [ratingsQuery.data, recipe.id, recipe.rating]);

  const saveRatingMutation = useMutation({
    mutationFn: async (next: { rating: number | null; favorite: boolean }) => {
      if (!currentUser || !recipe.slug) return;
      await setRecipeRating(currentUser, recipe.slug, next.rating, next.favorite);
    },
    onSuccess: async (_, variables) => {
      setRatingValue(variables.rating);
      setFavorite(variables.favorite);
      setStatus("Saved rating");
      await ratingsQuery.refetch();
      await onRecipeRefresh();
    },
    onError: saveError => {
      setError(saveError instanceof Error ? saveError.message : "Unable to save rating");
    },
  });

  const commentMutation = useMutation({
    mutationFn: async () => {
      if (!recipe.slug || !commentText.trim()) return;
      await addRecipeComment(recipe.slug, commentText.trim());
    },
    onSuccess: async () => {
      setCommentText("");
      setStatus("Comment added");
      await commentsQuery.refetch();
      await onRecipeRefresh();
    },
    onError: commentError => {
      setError(commentError instanceof Error ? commentError.message : "Unable to save comment");
    },
  });

  const shareMutation = useMutation({
    mutationFn: async () => {
      if (!recipe.slug) return;
      return await createRecipeShareToken(recipe.slug);
    },
    onSuccess: async () => {
      setStatus("Share link created");
      await sharesQuery.refetch();
    },
    onError: shareError => {
      setError(shareError instanceof Error ? shareError.message : "Unable to create share link");
    },
  });

  const imageMutation = useMutation({
    mutationFn: async (file: File) => {
      if (!recipe.slug) return;
      await uploadRecipeImage(recipe.slug, file);
    },
    onSuccess: async () => {
      setStatus("Recipe image updated");
      await onRecipeRefresh();
    },
    onError: imageError => {
      setError(imageError instanceof Error ? imageError.message : "Unable to upload image");
    },
  });

  const assetMutation = useMutation({
    mutationFn: async (file: File) => {
      if (!recipe.slug) return;
      await uploadRecipeAsset(recipe.slug, file);
    },
    onSuccess: async () => {
      setStatus("Recipe asset uploaded");
      await onRecipeRefresh();
    },
    onError: assetError => {
      setError(assetError instanceof Error ? assetError.message : "Unable to upload asset");
    },
  });

  const lastMadeMutation = useMutation({
    mutationFn: async () => {
      if (!recipe.slug) return;
      await updateLastMade(recipe.slug, new Date().toISOString());
    },
    onSuccess: async () => {
      setStatus("Marked as made");
      await onRecipeRefresh();
    },
    onError: lastMadeError => {
      setError(lastMadeError instanceof Error ? lastMadeError.message : "Unable to update recipe");
    },
  });

  const shareLinks = useMemo(() => {
    return (sharesQuery.data ?? []).map(token => ({
      ...token,
      href: buildShareUrl(groupSlug, token.id),
    }));
  }, [groupSlug, sharesQuery.data]);

  return (
    <Stack spacing={2}>
      {status ? <Alert severity="success" onClose={() => setStatus(null)}>{status}</Alert> : null}
      {error ? <Alert severity="error" onClose={() => setError(null)}>{error}</Alert> : null}

      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">Interactions</Typography>
            <Stack direction={{ xs: "column", md: "row" }} spacing={3} alignItems={{ md: "center" }}>
              <Box>
                <Typography variant="body2" color="text.secondary">Rating</Typography>
                <Rating
                  value={ratingValue}
                  onChange={(_, value) => {
                    setError(null);
                    saveRatingMutation.mutate({ rating: value, favorite });
                  }}
                />
              </Box>
              <Box>
                <Typography variant="body2" color="text.secondary">Favorite</Typography>
                <Switch
                  checked={favorite}
                  onChange={(_, checked) => {
                    setError(null);
                    saveRatingMutation.mutate({ rating: ratingValue, favorite: checked });
                  }}
                />
              </Box>
              <Button variant="outlined" onClick={() => lastMadeMutation.mutate()} disabled={lastMadeMutation.isPending}>
                Mark made now
              </Button>
            </Stack>

            <Divider />

            <Stack direction={{ xs: "column", md: "row" }} spacing={2}>
              <Button component="label" variant="outlined">
                Upload image
                <input
                  hidden
                  type="file"
                  accept="image/*"
                  onChange={event => {
                    const file = event.target.files?.[0];
                    if (file) {
                      imageMutation.mutate(file);
                    }
                  }}
                />
              </Button>
              <Button component="label" variant="outlined">
                Upload asset
                <input
                  hidden
                  type="file"
                  onChange={event => {
                    const file = event.target.files?.[0];
                    if (file) {
                      assetMutation.mutate(file);
                    }
                  }}
                />
              </Button>
              <Button variant="outlined" onClick={() => shareMutation.mutate()} disabled={shareMutation.isPending}>
                Create share link
              </Button>
            </Stack>

            {shareLinks.length ? (
              <Stack spacing={1}>
                <Typography variant="subtitle2">Share links</Typography>
                {shareLinks.map(link => (
                  <Chip
                    key={link.id}
                    component="a"
                    clickable
                    href={link.href}
                    label={link.href}
                    target="_blank"
                    rel="noreferrer"
                    sx={{ justifyContent: "flex-start" }}
                  />
                ))}
              </Stack>
            ) : null}

            {!recipe.settings?.disableComments ? (
              <>
                <Divider />
                <Typography variant="subtitle2">Comments</Typography>
                <TextField
                  label="Add comment"
                  multiline
                  minRows={3}
                  value={commentText}
                  onChange={event => setCommentText(event.target.value)}
                />
                <Button
                  variant="contained"
                  onClick={() => commentMutation.mutate()}
                  disabled={commentMutation.isPending || !commentText.trim()}
                >
                  Add comment
                </Button>
                <Stack spacing={1}>
                  {(commentsQuery.data ?? []).map(comment => (
                    <Card key={comment.id} variant="outlined">
                      <CardContent>
                        <Typography variant="subtitle2">{comment.user?.fullName ?? comment.user?.username ?? "Mealie user"}</Typography>
                        <Typography variant="body2" color="text.secondary">
                          {new Date(comment.createdAt).toLocaleString()}
                        </Typography>
                        <Typography sx={{ mt: 1 }}>{comment.text}</Typography>
                      </CardContent>
                    </Card>
                  ))}
                </Stack>
              </>
            ) : null}
          </Stack>
        </CardContent>
      </Card>
    </Stack>
  );
}
