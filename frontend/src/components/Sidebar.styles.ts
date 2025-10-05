import { Drawer, Typography, Button, ListItemButton } from '@mui/material'
import { styled } from '@mui/material/styles'

export const StyledDrawer = styled(Drawer)(({ theme }) => ({
  '& .MuiDrawer-paper': {
    backgroundColor: theme.palette.background.paper,
    width: 280,
    overflow: 'hidden',
    display: 'flex',
    flexDirection: 'column',
    zIndex: 1400,
    position: 'fixed',
    top: 0,
    left: 0,
    height: '100vh',
    boxShadow: '2px 0 8px rgba(0, 0, 0, 0.3)',
  },
}))

export const Logo = styled(Typography)(({ theme }) => ({
  fontWeight: 'bold',
  fontSize: '1.5rem',
  color: theme.palette.text.primary,
  padding: '16px',
  textAlign: 'center',
  borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
}))

export const CollapseButton = styled(Button)(({ theme }) => ({
  minWidth: 'auto',
  padding: '8px',
  color: theme.palette.text.primary,
  '&:hover': {
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
  },
}))

export const DownloadButton = styled(Button)(({ theme }) => ({
  backgroundColor: theme.palette.primary.main,
  color: theme.palette.primary.contrastText,
  textTransform: 'none',
  fontWeight: 600,
  padding: '12px 16px',
  margin: '16px',
  borderRadius: '8px',
  '&:hover': {
    backgroundColor: theme.palette.primary.light,
  },
}))

export const ActiveItem = styled(ListItemButton)(({ theme }) => ({
  backgroundColor: `${theme.palette.primary.main}20`,
  borderLeft: `3px solid ${theme.palette.primary.main}`,
  color: `${theme.palette.text.primary} !important`,
  '&:hover': {
    backgroundColor: `${theme.palette.primary.main}30`,
  },
  '& .MuiListItemText-primary': {
    color: `${theme.palette.text.primary} !important`,
  },
}))

export const InactiveItem = styled(ListItemButton)(({ theme }) => ({
  color: theme.palette.text.primary,
  '&:hover': {
    backgroundColor: 'rgba(255, 255, 255, 0.05)',
  },
  '& .MuiListItemText-primary': {
    color: `${theme.palette.text.primary} !important`,
  },
}))
