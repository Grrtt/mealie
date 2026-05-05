import { z } from "zod";

export const emailSchema = z.string().trim().email();
export const passwordSchema = z.string().min(8);

export const loginSchema = z.object({
  username: z.string().trim().min(1),
  password: passwordSchema,
  rememberMe: z.boolean().default(false),
});

export const forgotPasswordSchema = z.object({
  email: emailSchema,
});

export const resetPasswordSchema = z.object({
  email: emailSchema.optional(),
  password: passwordSchema,
  passwordConfirm: passwordSchema,
  token: z.string().trim().min(1),
}).refine(data => data.password === data.passwordConfirm, {
  message: "Passwords must match",
  path: ["passwordConfirm"],
});

export const registerSchema = z.object({
  email: emailSchema,
  username: z.string().trim().min(1),
  fullName: z.string().trim().min(1),
  password: passwordSchema,
  passwordConfirm: passwordSchema,
  groupToken: z.string().trim().optional(),
}).refine(data => data.password === data.passwordConfirm, {
  message: "Passwords must match",
  path: ["passwordConfirm"],
});
