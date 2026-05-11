import { type ComponentPropsWithoutRef, useEffect, useState } from "react";
import RestaurantRoundedIcon from "@mui/icons-material/RestaurantRounded";
import Box from "@mui/material/Box";
import Skeleton from "@mui/material/Skeleton";
import Stack from "@mui/material/Stack";
import { alpha, type SxProps, type Theme } from "@mui/material/styles";

type Props = {
  src?: string | null;
  alt: string;
  wrapperSx?: SxProps<Theme>;
  imageSx?: SxProps<Theme>;
  imgProps?: Omit<ComponentPropsWithoutRef<"img">, "src" | "alt"> & Record<`data-${string}`, string | number | undefined>;
};

type ImageStatus = "idle" | "loading" | "retrying" | "loaded" | "error";

const IMAGE_RETRY_DELAY_MS = 2000;
const MAX_IMAGE_RETRY_ATTEMPTS = 30;

function buildAttemptedSrc(src: string, attempt: number) {
  const separator = src.includes("?") ? "&" : "?";
  return `${src}${separator}refresh=${attempt}`;
}

export function RecipeImage({ src, alt, wrapperSx, imageSx, imgProps }: Props) {
  const [status, setStatus] = useState<ImageStatus>(src ? "loading" : "idle");
  const [resolvedSrc, setResolvedSrc] = useState<string | null>(null);
  const [isVisible, setIsVisible] = useState(false);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    setAttempt(0);
  }, [src]);

  useEffect(() => {
    let cancelled = false;
    let frame: number | null = null;
    let retryTimer: number | null = null;

    setResolvedSrc(null);
    setIsVisible(false);

    if (!src) {
      setStatus("idle");
      return;
    }

    setStatus(attempt === 0 ? "loading" : "retrying");

    const requestSrc = buildAttemptedSrc(src, attempt);

    const image = new window.Image();
    image.decoding = "async";

    const markLoaded = () => {
      if (cancelled) return;
      setResolvedSrc(requestSrc);
      setStatus("loaded");
      frame = window.requestAnimationFrame(() => {
        if (!cancelled) {
          setIsVisible(true);
        }
      });
    };

    const markErrored = () => {
      if (cancelled) return;
      setResolvedSrc(null);
      setIsVisible(false);
      setStatus("error");
      if (attempt < MAX_IMAGE_RETRY_ATTEMPTS) {
        retryTimer = window.setTimeout(() => {
          if (!cancelled) {
            setAttempt(current => current + 1);
          }
        }, IMAGE_RETRY_DELAY_MS);
      }
    };

    image.onload = markLoaded;
    image.onerror = markErrored;
    image.src = requestSrc;

    if (image.complete) {
      if (image.naturalWidth > 0) {
        markLoaded();
      } else {
        markErrored();
      }
    }

    return () => {
      cancelled = true;
      if (frame != null) {
        window.cancelAnimationFrame(frame);
      }
      if (retryTimer != null) {
        window.clearTimeout(retryTimer);
      }
      image.onload = null;
      image.onerror = null;
    };
  }, [attempt, src]);

  const isLoading = Boolean(src) && (status === "loading" || status === "retrying");
  const showPlaceholder = !resolvedSrc;

  return (
    <Box
      aria-label={showPlaceholder ? alt : undefined}
      role={showPlaceholder ? "img" : undefined}
      sx={[
        {
          position: "relative",
          overflow: "hidden",
          width: "100%",
          height: "100%",
          bgcolor: "grey.100",
        },
        ...(Array.isArray(wrapperSx) ? wrapperSx : wrapperSx ? [wrapperSx] : []),
      ]}
    >
      <Box
        aria-hidden="true"
        sx={{
          position: "absolute",
          inset: 0,
          opacity: showPlaceholder ? 1 : 0,
          transition: theme => theme.transitions.create("opacity", {
            duration: theme.transitions.duration.standard,
            easing: theme.transitions.easing.easeOut,
          }),
        }}
      >
        {isLoading ? (
          <Skeleton animation="wave" height="100%" variant="rectangular" width="100%" />
        ) : (
          <Stack
            alignItems="center"
            justifyContent="center"
            spacing={1}
            sx={{
              width: "100%",
              height: "100%",
              color: "text.secondary",
              background: theme => theme.palette.mode === "dark"
                ? `linear-gradient(160deg, ${alpha(theme.palette.background.paper, 0.88)} 0%, ${alpha(theme.palette.background.default, 0.92)} 100%)`
                : `linear-gradient(160deg, ${theme.palette.grey[100]} 0%, ${theme.palette.common.white} 100%)`,
            }}
          >
            <RestaurantRoundedIcon sx={{ fontSize: 40, opacity: 0.55 }} />
          </Stack>
        )}
      </Box>

      {resolvedSrc ? (
        <Box
          component="img"
          src={resolvedSrc}
          alt={alt}
          {...imgProps}
          sx={[
            {
              position: "relative",
              zIndex: 1,
              width: "100%",
              height: "100%",
              display: "block",
              objectFit: "cover",
              opacity: isVisible ? 1 : 0,
              transition: theme => theme.transitions.create("opacity", {
                duration: theme.transitions.duration.standard,
                easing: theme.transitions.easing.easeOut,
              }),
            },
            ...(Array.isArray(imageSx) ? imageSx : imageSx ? [imageSx] : []),
          ]}
        />
      ) : null}
    </Box>
  );
}
