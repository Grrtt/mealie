import { useState } from "react";
import AdminPanelSettingsRoundedIcon from "@mui/icons-material/AdminPanelSettingsRounded";
import AddBoxRoundedIcon from "@mui/icons-material/AddBoxRounded";
import DatasetRoundedIcon from "@mui/icons-material/DatasetRounded";
import ImportExportRoundedIcon from "@mui/icons-material/ImportExportRounded";
import MenuRoundedIcon from "@mui/icons-material/MenuRounded";
import SettingsRoundedIcon from "@mui/icons-material/SettingsRounded";
import AppBar from "@mui/material/AppBar";
import Avatar from "@mui/material/Avatar";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Container from "@mui/material/Container";
import Divider from "@mui/material/Divider";
import Drawer from "@mui/material/Drawer";
import IconButton from "@mui/material/IconButton";
import List from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import Stack from "@mui/material/Stack";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import useMediaQuery from "@mui/material/useMediaQuery";
import PersonRoundedIcon from "@mui/icons-material/PersonRounded";
import { useTheme } from "@mui/material/styles";
import { MainNav } from "@/components/navigation/MainNav";
import { LogoutButton } from "@/components/auth/LogoutButton";
import { useCurrentUser } from "@/features/auth/useCurrentUser";
import { apiClient } from "@/lib/api/client";

type Props = {
  groupSlug: string;
  title?: string;
  userName?: string | null;
  children: React.ReactNode;
};

const drawerWidth = 280;

export function AppShell({ groupSlug, title = "Mealie", userName, children }: Props) {
  const theme = useTheme();
  const isDesktop = useMediaQuery(theme.breakpoints.up("lg"));
  const [mobileSidebarOpen, setMobileSidebarOpen] = useState(false);
  const [settingsAnchor, setSettingsAnchor] = useState<HTMLElement | null>(null);
  const { data: currentUser } = useCurrentUser();
  const displayName = userName ?? currentUser?.fullName ?? currentUser?.username ?? null;
  const canManage = Boolean(currentUser?.canManage);
  const isAdmin = Boolean(currentUser?.admin);

  const initials = displayName
    ? displayName
      .split(" ")
      .map(part => part[0])
      .join("")
      .slice(0, 2)
      .toUpperCase()
    : "M";

  const drawer = (
    <Box sx={{ display: "flex", height: "100%", flexDirection: "column" }}>
      <Box sx={{ px: 2, py: 2 }}>
        {displayName ? (
          <Stack
            component="a"
            direction="row"
            spacing={1.5}
            href={apiClient.resolvePath("/user/profile")}
            sx={{
              alignItems: "center",
              color: "inherit",
              textDecoration: "none",
              borderRadius: 2,
              px: 1,
              py: 1,
              "&:hover": { bgcolor: "action.hover" },
            }}
          >
            <Avatar sx={{ bgcolor: "primary.main", color: "primary.contrastText" }}>
              {initials}
            </Avatar>
            <Box sx={{ minWidth: 0 }}>
              <Typography noWrap fontWeight={600} variant="body2">
                {displayName}
              </Typography>
              <Typography color="text.secondary" noWrap variant="caption">
                User settings
              </Typography>
            </Box>
          </Stack>
        ) : null}
        <Button
          fullWidth
          href={apiClient.resolvePath(`/g/${groupSlug}/r/create`)}
          startIcon={<AddBoxRoundedIcon />}
          sx={{ mt: displayName ? 2 : 0.5, justifyContent: "flex-start", px: 1.5, py: 1.25 }}
          variant="contained"
        >
          Create
        </Button>
      </Box>
      <Divider />
      <Box sx={{ flexGrow: 1, overflowY: "auto", px: 1.5, py: 2 }}>
        <MainNav
          groupSlug={groupSlug}
          onNavigate={() => {
            if (!isDesktop) {
              setMobileSidebarOpen(false);
            }
          }}
        />
      </Box>
      <Divider />
      <Stack spacing={2} sx={{ p: 2 }}>
        <List sx={{ p: 0 }}>
          <ListItemButton
            aria-controls={settingsAnchor ? "app-shell-settings-menu" : undefined}
            aria-expanded={settingsAnchor ? "true" : undefined}
            aria-haspopup="menu"
            onClick={event => setSettingsAnchor(event.currentTarget)}
            sx={{ borderRadius: 2 }}
          >
            <ListItemIcon sx={{ minWidth: 40 }}>
              <SettingsRoundedIcon />
            </ListItemIcon>
            <ListItemText primary="Settings" />
          </ListItemButton>
        </List>
        <Menu
          id="app-shell-settings-menu"
          anchorEl={settingsAnchor}
          open={Boolean(settingsAnchor)}
          onClose={() => setSettingsAnchor(null)}
          anchorOrigin={{ horizontal: "right", vertical: "top" }}
          transformOrigin={{ horizontal: "left", vertical: "bottom" }}
        >
          <MenuItem
            component="a"
            href={apiClient.resolvePath("/user/profile")}
            onClick={() => setSettingsAnchor(null)}
          >
            <ListItemIcon>
              <PersonRoundedIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText>User settings</ListItemText>
          </MenuItem>
          {canManage ? <Divider /> : null}
          {canManage ? (
            <MenuItem
              component="a"
              href={apiClient.resolvePath("/group/data")}
              onClick={() => setSettingsAnchor(null)}
            >
              <ListItemIcon>
                <DatasetRoundedIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText>Data management</ListItemText>
            </MenuItem>
          ) : null}
          {canManage ? (
            <MenuItem
              component="a"
              href={apiClient.resolvePath("/group/migrations")}
              onClick={() => setSettingsAnchor(null)}
            >
                <ListItemIcon>
                  <ImportExportRoundedIcon fontSize="small" />
                </ListItemIcon>
              <ListItemText>Migrations</ListItemText>
            </MenuItem>
          ) : null}
          {isAdmin ? <Divider /> : null}
          {isAdmin ? (
            <MenuItem
              component="a"
              href={apiClient.resolvePath("/admin/site-settings")}
              onClick={() => setSettingsAnchor(null)}
            >
              <ListItemIcon>
                <AdminPanelSettingsRoundedIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText>Admin settings</ListItemText>
            </MenuItem>
          ) : null}
        </Menu>
        <LogoutButton color="primary" fullWidth variant="outlined" />
      </Stack>
    </Box>
  );

  return (
    <Box sx={{ display: "flex", minHeight: "100vh", bgcolor: "background.default" }}>
      <AppBar
        color="inherit"
        elevation={0}
        position="fixed"
        sx={{
          borderBottom: 1,
          borderColor: "divider",
          bgcolor: "primary.main",
          color: "primary.contrastText",
          width: { lg: `calc(100% - ${drawerWidth}px)` },
          ml: { lg: `${drawerWidth}px` },
        }}
      >
        <Toolbar sx={{ gap: 2, py: 1 }}>
          <IconButton
            aria-label="Open navigation menu"
            edge="start"
            onClick={() => setMobileSidebarOpen(true)}
            sx={{ color: "inherit", display: { lg: "none" } }}
          >
            <MenuRoundedIcon />
          </IconButton>
          <Box sx={{ minWidth: 0, flexGrow: 1 }}>
            <Typography noWrap variant="h6">
              {title}
            </Typography>
            {displayName ? (
              <Typography color="text.secondary" noWrap variant="body2">
                {displayName}
              </Typography>
            ) : null}
          </Box>
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        sx={{ width: { lg: drawerWidth }, flexShrink: { lg: 0 } }}
      >
        <Drawer
          ModalProps={{ keepMounted: true }}
          onClose={() => setMobileSidebarOpen(false)}
          open={!isDesktop && mobileSidebarOpen}
          sx={{
            display: { xs: "block", lg: "none" },
            "& .MuiDrawer-paper": {
              boxSizing: "border-box",
              width: drawerWidth,
            },
          }}
          variant="temporary"
        >
          {drawer}
        </Drawer>
        <Drawer
          open
          sx={{
            display: { xs: "none", lg: "block" },
            "& .MuiDrawer-paper": {
              boxSizing: "border-box",
              width: drawerWidth,
              borderRight: 1,
              borderRightColor: "divider",
            },
          }}
          variant="permanent"
        >
          {drawer}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          width: { lg: `calc(100% - ${drawerWidth}px)` },
        }}
      >
        <Toolbar />
        <Container component="section" maxWidth="xl" sx={{ py: 4 }}>
          {children}
        </Container>
      </Box>
    </Box>
  );
}
