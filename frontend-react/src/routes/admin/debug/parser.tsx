import { useState } from "react";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import TextField from "@mui/material/TextField";
import Typography from "@mui/material/Typography";
import { useMutation } from "@tanstack/react-query";
import { SettingsPage } from "@/components/settings/SettingsPage";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { parseIngredient } from "@/features/settings/api";

export function AdminDebugParserRouteComponent() {
  const { data: user } = useCurrentUser();
  const [parser, setParser] = useState<"nlp" | "brute" | "openai">("nlp");
  const [ingredient, setIngredient] = useState("2 tbsp minced cilantro");
  const mutation = useMutation({
    mutationFn: async () => await parseIngredient(parser, ingredient),
  });

  return (
    <SettingsPage user={user} title="Parser debug" description="Exercise the ingredient parser variants against representative ingredient text.">
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <TextField select label="Parser" value={parser} onChange={event => setParser(event.target.value as "nlp" | "brute" | "openai")}>
              <MenuItem value="nlp">NLP</MenuItem>
              <MenuItem value="brute">Brute</MenuItem>
              <MenuItem value="openai">OpenAI</MenuItem>
            </TextField>
            <TextField label="Ingredient" value={ingredient} onChange={event => setIngredient(event.target.value)} />
            <Stack direction="row" justifyContent="flex-end">
              <Button variant="contained" onClick={() => mutation.mutate()}>Run parser</Button>
            </Stack>
            {mutation.data ? (
              <Typography component="pre" sx={{ whiteSpace: "pre-wrap", fontFamily: "monospace", fontSize: 12 }}>
                {JSON.stringify(mutation.data, null, 2)}
              </Typography>
            ) : null}
          </Stack>
        </CardContent>
      </Card>
    </SettingsPage>
  );
}
