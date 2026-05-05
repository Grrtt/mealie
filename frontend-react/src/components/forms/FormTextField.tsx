import { Controller, type Control, type FieldPath, type FieldValues } from "react-hook-form";
import TextField, { type TextFieldProps } from "@mui/material/TextField";

type Props<TFieldValues extends FieldValues> = {
  control: Control<TFieldValues>;
  name: FieldPath<TFieldValues>;
} & Omit<TextFieldProps, "name">;

export function FormTextField<TFieldValues extends FieldValues>({
  control,
  name,
  ...props
}: Props<TFieldValues>) {
  return (
    <Controller
      control={control}
      name={name}
      render={({ field, fieldState }) => (
        <TextField
          {...props}
          {...field}
          value={field.value ?? ""}
          error={Boolean(fieldState.error)}
          helperText={fieldState.error?.message ?? props.helperText}
        />
      )}
    />
  );
}
