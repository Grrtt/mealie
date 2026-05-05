import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, type DefaultValues, type UseFormProps } from "react-hook-form";
import type { z } from "zod";

export function useZodForm<TFieldValues extends Record<string, unknown> = Record<string, unknown>>(
  schema: z.ZodTypeAny,
  options?: Omit<UseFormProps<TFieldValues>, "resolver"> & {
    defaultValues?: DefaultValues<TFieldValues>;
  },
) {
  return useForm<TFieldValues>({
    ...options,
    resolver: zodResolver(schema as never) as never,
  });
}
