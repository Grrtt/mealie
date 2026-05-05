import { type ReactNode, useMemo, useState } from "react";
import Button from "@mui/material/Button";
import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import Stack from "@mui/material/Stack";
import Typography from "@mui/material/Typography";

type Props<T extends { id: string }> = {
  title: string;
  createLabel: string;
  items: T[];
  initialDraft: T;
  renderFields: (draft: T, setDraft: (updater: (current: T) => T) => void, mode: "create" | "edit") => ReactNode;
  getPrimaryText: (item: T) => string;
  getSecondaryText?: (item: T) => string | null | undefined;
  onCreate: (draft: T) => void;
  onSave: (draft: T) => void;
  onDelete: (draft: T) => void;
};

export function SimpleCrudList<T extends { id: string }>({
  title,
  createLabel,
  items,
  initialDraft,
  renderFields,
  getPrimaryText,
  getSecondaryText,
  onCreate,
  onSave,
  onDelete,
}: Props<T>) {
  const [createDraft, setCreateDraft] = useState<T>(initialDraft);
  const [editing, setEditing] = useState<Record<string, T>>({});

  const itemDrafts = useMemo(() => items.map(item => editing[item.id] ?? item), [editing, items]);

  return (
    <Stack spacing={2}>
      <Card>
        <CardContent>
          <Stack spacing={2}>
            <Typography variant="h6">{createLabel}</Typography>
            {renderFields(createDraft, updater => setCreateDraft(current => updater(current)), "create")}
            <Stack direction="row" justifyContent="flex-end">
              <Button
                variant="contained"
                onClick={() => {
                  onCreate(createDraft);
                  setCreateDraft(initialDraft);
                }}
              >
                Create
              </Button>
            </Stack>
          </Stack>
        </CardContent>
      </Card>

      <Typography variant="h6">{title}</Typography>
      {itemDrafts.map(item => (
        <Card key={item.id} variant="outlined">
          <CardContent>
            <Stack spacing={2}>
              <Stack spacing={0.5}>
                <Typography variant="h6">{getPrimaryText(item)}</Typography>
                {getSecondaryText?.(item) ? (
                  <Typography color="text.secondary">{getSecondaryText(item)}</Typography>
                ) : null}
              </Stack>
              {renderFields(
                item,
                updater => setEditing(current => ({ ...current, [item.id]: updater(current[item.id] ?? item) })),
                "edit",
              )}
              <Stack direction="row" spacing={1} justifyContent="flex-end">
                <Button variant="contained" onClick={() => onSave(item)}>Save</Button>
                <Button color="error" variant="outlined" onClick={() => onDelete(item)}>Delete</Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      ))}
    </Stack>
  );
}
