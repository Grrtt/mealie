import { useMemo, useState } from "react";
import AdminPanelSettingsRoundedIcon from "@mui/icons-material/AdminPanelSettingsRounded";
import BackupRoundedIcon from "@mui/icons-material/BackupRounded";
import BuildRoundedIcon from "@mui/icons-material/BuildRounded";
import CategoryRoundedIcon from "@mui/icons-material/CategoryRounded";
import ExpandLessRoundedIcon from "@mui/icons-material/ExpandLessRounded";
import ExpandMoreRoundedIcon from "@mui/icons-material/ExpandMoreRounded";
import GroupRoundedIcon from "@mui/icons-material/GroupRounded";
import GroupsRoundedIcon from "@mui/icons-material/GroupsRounded";
import HomeRoundedIcon from "@mui/icons-material/HomeRounded";
import ManageSearchRoundedIcon from "@mui/icons-material/ManageSearchRounded";
import MenuBookRoundedIcon from "@mui/icons-material/MenuBookRounded";
import PeopleRoundedIcon from "@mui/icons-material/PeopleRounded";
import PlumbingRoundedIcon from "@mui/icons-material/PlumbingRounded";
import PsychologyRoundedIcon from "@mui/icons-material/PsychologyRounded";
import SellRoundedIcon from "@mui/icons-material/SellRounded";
import ShoppingCartRoundedIcon from "@mui/icons-material/ShoppingCartRounded";
import SmartToyRoundedIcon from "@mui/icons-material/SmartToyRounded";
import SoupKitchenRoundedIcon from "@mui/icons-material/SoupKitchenRounded";
import StorageRoundedIcon from "@mui/icons-material/StorageRounded";
import TimelineRoundedIcon from "@mui/icons-material/TimelineRounded";
import TodayRoundedIcon from "@mui/icons-material/TodayRounded";
import WarningRoundedIcon from "@mui/icons-material/WarningRounded";
import Box from "@mui/material/Box";
import Collapse from "@mui/material/Collapse";
import Divider from "@mui/material/Divider";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import { Link } from "@tanstack/react-router";
import type { ReactNode } from "react";
import { alpha } from "@mui/material/styles";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { apiClient } from "@/lib/api/client";

type Props = {
  groupSlug: string;
  onNavigate?: () => void;
};

type NavLinkItem = {
  type: "item";
  href: string;
  icon: ReactNode;
  label: string;
  matches: (pathname: string) => boolean;
  hidden?: boolean;
};

type NavGroupItem = {
  type: "group";
  key: string;
  icon: ReactNode;
  label: string;
  children: NavLinkItem[];
  hidden?: boolean;
};

type NavItem = NavLinkItem | NavGroupItem;

function isGroup(item: NavItem): item is NavGroupItem {
  return item.type === "group";
}

function isItem(item: NavItem): item is NavLinkItem {
  return item.type === "item";
}

function matchesAny(pathname: string, items: NavLinkItem[]) {
  return items.some(item => item.matches(pathname));
}

export function MainNav({ groupSlug, onNavigate }: Props) {
  const { data: currentUser } = useCurrentUser();
  const currentPath = typeof window === "undefined" ? "" : window.location.pathname;
  const isAdminRoute = currentPath.startsWith(apiClient.resolvePath("/admin"));

  const recipesRoot = apiClient.resolvePath(`/g/${groupSlug}/recipes`);
  const recipeFinder = apiClient.resolvePath(`/g/${groupSlug}/recipes/finder`);
  const recipeTimeline = apiClient.resolvePath(`/g/${groupSlug}/recipes/timeline`);
  const cookbooksRoot = apiClient.resolvePath(`/g/${groupSlug}/cookbooks`);
  const categoriesRoot = apiClient.resolvePath(`/g/${groupSlug}/recipes/categories`);
  const tagsRoot = apiClient.resolvePath(`/g/${groupSlug}/recipes/tags`);
  const toolsRoot = apiClient.resolvePath(`/g/${groupSlug}/recipes/tools`);
  const mealPlannerRoot = apiClient.resolvePath("/household/mealplan");
  const shoppingListsRoot = apiClient.resolvePath("/shopping-lists");
  const adminRoot = apiClient.resolvePath("/admin");

  const defaultItems = useMemo<NavItem[]>(() => [
    {
      type: "item",
      label: "Recipes",
      href: recipesRoot,
      icon: <HomeRoundedIcon />,
      matches: (pathname) => {
        if (pathname === recipesRoot) return true;
        if (pathname.startsWith(apiClient.resolvePath(`/g/${groupSlug}/r/`))) return true;
        return pathname.startsWith(`${recipesRoot}/`)
          && !pathname.startsWith(recipeFinder)
          && !pathname.startsWith(recipeTimeline)
          && !pathname.startsWith(categoriesRoot)
          && !pathname.startsWith(tagsRoot)
          && !pathname.startsWith(toolsRoot);
      },
    },
    {
      type: "item",
      label: "Finder",
      href: recipeFinder,
      icon: <ManageSearchRoundedIcon />,
      matches: pathname => pathname.startsWith(recipeFinder),
    },
    {
      type: "item",
      label: "Meal planner",
      href: apiClient.resolvePath("/household/mealplan/planner/view"),
      icon: <TodayRoundedIcon />,
      matches: pathname => pathname.startsWith(mealPlannerRoot),
    },
    {
      type: "item",
      label: "Shopping lists",
      href: shoppingListsRoot,
      icon: <ShoppingCartRoundedIcon />,
      matches: pathname => pathname.startsWith(shoppingListsRoot),
    },
    {
      type: "item",
      label: "Timeline",
      href: recipeTimeline,
      icon: <TimelineRoundedIcon />,
      matches: pathname => pathname.startsWith(recipeTimeline),
    },
    {
      type: "item",
      label: "Cookbooks",
      href: cookbooksRoot,
      icon: <MenuBookRoundedIcon />,
      matches: pathname => pathname.startsWith(cookbooksRoot),
    },
    {
      type: "group",
      key: "organizers",
      label: "Organizers",
      icon: <StorageRoundedIcon />,
      children: [
        {
          type: "item",
          label: "Categories",
          href: categoriesRoot,
          icon: <CategoryRoundedIcon />,
          matches: pathname => pathname.startsWith(categoriesRoot),
        },
        {
          type: "item",
          label: "Tags",
          href: tagsRoot,
          icon: <SellRoundedIcon />,
          matches: pathname => pathname.startsWith(tagsRoot),
        },
        {
          type: "item",
          label: "Tools",
          href: toolsRoot,
          icon: <SoupKitchenRoundedIcon />,
          matches: pathname => pathname.startsWith(toolsRoot),
        },
      ],
    },
  ], [categoriesRoot, cookbooksRoot, groupSlug, mealPlannerRoot, recipeFinder, recipeTimeline, recipesRoot, shoppingListsRoot, tagsRoot, toolsRoot]);

  const adminPrimaryItems = useMemo<NavItem[]>(() => [
    {
      type: "item",
      label: "Site settings",
      href: apiClient.resolvePath("/admin/site-settings"),
      icon: <AdminPanelSettingsRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/site-settings")),
    },
    {
      type: "item",
      label: "Users",
      href: apiClient.resolvePath("/admin/manage/users"),
      icon: <PeopleRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/manage/users")),
    },
    {
      type: "item",
      label: "Households",
      href: apiClient.resolvePath("/admin/manage/households"),
      icon: <HomeRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/manage/households")),
    },
    {
      type: "item",
      label: "Groups",
      href: apiClient.resolvePath("/admin/manage/groups"),
      icon: <GroupsRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/manage/groups")),
    },
    {
      type: "item",
      label: "AI configurations",
      href: apiClient.resolvePath("/admin/ai-configurations"),
      icon: <PsychologyRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/ai-configurations")),
    },
    {
      type: "item",
      label: "Backups",
      href: apiClient.resolvePath("/admin/backups"),
      icon: <BackupRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/backups")),
    },
    {
      type: "item",
      label: "Logs",
      href: apiClient.resolvePath("/admin/logs"),
      icon: <WarningRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/logs")),
    },
    {
      type: "item",
      label: "Ingredient aliases",
      href: apiClient.resolvePath("/admin/manage/ingredient-aliases"),
      icon: <GroupRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/manage/ingredient-aliases")),
    },
  ], []);

  const adminSecondaryItems = useMemo<NavItem[]>(() => [
    {
      type: "item",
      label: "Maintenance",
      href: apiClient.resolvePath("/admin/maintenance"),
      icon: <BuildRoundedIcon />,
      matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/maintenance")),
    },
    {
      type: "group",
      key: "admin-debug",
      label: "Debug",
      icon: <SmartToyRoundedIcon />,
      children: [
        {
          type: "item",
          label: "OpenAI",
          href: apiClient.resolvePath("/admin/debug/openai"),
          icon: <PsychologyRoundedIcon />,
          matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/debug/openai")),
        },
        {
          type: "item",
          label: "Parser",
          href: apiClient.resolvePath("/admin/debug/parser"),
          icon: <PlumbingRoundedIcon />,
          matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/debug/parser")),
        },
        {
          type: "item",
          label: "Search indexes",
          href: apiClient.resolvePath("/admin/debug/indexes"),
          icon: <ManageSearchRoundedIcon />,
          matches: pathname => pathname.startsWith(apiClient.resolvePath("/admin/debug/indexes")),
        },
      ],
    },
  ], []);

  const visiblePrimaryItems = (isAdminRoute ? adminPrimaryItems : defaultItems).filter(item => !item.hidden);
  const visibleSecondaryItems = (isAdminRoute ? adminSecondaryItems : []).filter(item => !item.hidden);
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>(() => ({
    organizers: matchesAny(currentPath, isGroup(defaultItems[6]!) ? defaultItems[6].children : []),
    "admin-debug": matchesAny(currentPath, isGroup(adminSecondaryItems[1]!) ? adminSecondaryItems[1].children : []),
  }));

  function toggleGroup(key: string, defaultExpanded: boolean) {
    setExpandedGroups(current => ({
      ...current,
      [key]: !(current[key] ?? defaultExpanded),
    }));
  }

  function renderItems(items: NavItem[], nested = false) {
    return items.map(item => {
      if (isItem(item)) {
        const selected = item.matches(currentPath);

        return (
          <ListItemButton
            key={item.label}
            component={Link}
            to={item.href}
            onClick={onNavigate}
            selected={selected}
            sx={{
              borderRadius: 2.5,
              px: nested ? 2 : 1.5,
              py: 1.05,
              ml: nested ? 2 : 0,
              color: selected ? "primary.dark" : "text.primary",
              bgcolor: selected ? theme => alpha(theme.palette.primary.main, 0.12) : "transparent",
              "&:hover": {
                bgcolor: theme => selected
                  ? alpha(theme.palette.primary.main, 0.16)
                  : alpha(theme.palette.primary.main, 0.05),
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 40, color: selected ? "primary.dark" : "text.secondary" }}>
              {item.icon}
            </ListItemIcon>
            <ListItemText
              primary={item.label}
              primaryTypographyProps={{ fontWeight: selected ? 700 : 500 }}
            />
          </ListItemButton>
        );
      }

      const selected = matchesAny(currentPath, item.children);
      const expanded = expandedGroups[item.key] ?? selected;

      return (
        <Box key={item.key}>
          <ListItemButton
            onClick={() => toggleGroup(item.key, selected)}
            selected={selected}
            sx={{
              borderRadius: 2.5,
              px: nested ? 2 : 1.5,
              py: 1.05,
              color: selected ? "primary.dark" : "text.primary",
              bgcolor: selected ? theme => alpha(theme.palette.primary.main, 0.1) : "transparent",
              "&:hover": {
                bgcolor: theme => selected
                  ? alpha(theme.palette.primary.main, 0.14)
                  : alpha(theme.palette.primary.main, 0.05),
              },
            }}
          >
            <ListItemIcon sx={{ minWidth: 40, color: selected ? "primary.dark" : "text.secondary" }}>
              {item.icon}
            </ListItemIcon>
            <ListItemText
              primary={item.label}
              primaryTypographyProps={{ fontWeight: selected ? 700 : 500 }}
            />
            {expanded ? <ExpandLessRoundedIcon fontSize="small" /> : <ExpandMoreRoundedIcon fontSize="small" />}
          </ListItemButton>
          <Collapse in={expanded} timeout="auto" unmountOnExit>
            <List disablePadding sx={{ pt: 0.5 }}>
              {renderItems(item.children, true)}
            </List>
          </Collapse>
        </Box>
      );
    });
  }

  return (
    <Box component="nav" aria-label={isAdminRoute ? "Admin navigation" : "Primary navigation"}>
      {isAdminRoute && currentUser?.admin ? (
        <List sx={{ display: "grid", gap: 0.5, p: 0 }}>
          <ListItemButton
            component={Link}
            to="/admin"
            onClick={onNavigate}
            selected={currentPath === adminRoot || currentPath === `${adminRoot}/setup`}
            sx={{
              borderRadius: 2.5,
              px: 1.5,
              py: 1.05,
              color: currentPath === adminRoot || currentPath === `${adminRoot}/setup` ? "primary.dark" : "text.primary",
              bgcolor: currentPath === adminRoot || currentPath === `${adminRoot}/setup`
                ? theme => alpha(theme.palette.primary.main, 0.12)
                : "transparent",
              "&:hover": {
                bgcolor: theme => currentPath === adminRoot || currentPath === `${adminRoot}/setup`
                  ? alpha(theme.palette.primary.main, 0.16)
                  : alpha(theme.palette.primary.main, 0.05),
              },
            }}
          >
            <ListItemIcon
              sx={{
                minWidth: 40,
                color: currentPath === adminRoot || currentPath === `${adminRoot}/setup` ? "primary.dark" : "text.secondary",
              }}
            >
              <StorageRoundedIcon />
            </ListItemIcon>
            <ListItemText primary="Admin setup" primaryTypographyProps={{ fontWeight: currentPath === adminRoot || currentPath === `${adminRoot}/setup` ? 700 : 500 }} />
          </ListItemButton>
          {renderItems(visiblePrimaryItems)}
        </List>
      ) : (
        <List sx={{ display: "grid", gap: 0.5, p: 0 }}>
          {renderItems(visiblePrimaryItems)}
        </List>
      )}

      {visibleSecondaryItems.length > 0 ? (
        <>
          <Divider sx={{ my: 1.5 }} />
          <List sx={{ display: "grid", gap: 0.5, p: 0 }}>
            {renderItems(visibleSecondaryItems)}
          </List>
        </>
      ) : null}
    </Box>
  );
}
