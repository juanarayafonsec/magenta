import { 
  List, 
  ListItem, 
  ListItemButton, 
  ListItemIcon, 
  ListItemText, 
  Collapse,
  Box,
  Button,
  Typography
} from '@mui/material'
import { 
  Casino as CasinoIcon,
  Sports as SportsIcon,
  CardGiftcard as PromotionsIcon,
  Telegram as TelegramIcon,
  ShoppingCart as BuyCryptoIcon,
  Help as HelpIcon,
  ExpandLess,
  ExpandMore,
  ArrowBack as CollapseIcon,
  ArrowForward as ExpandIcon,
  VideogameAsset as SlotsIcon,
  LiveTv as LiveCasinoIcon,
  Casino as GameShowsIcon,
  Rocket as CasualGamesIcon
} from '@mui/icons-material'
import { useState } from 'react'
import { StyledDrawer, Logo, CollapseButton, DownloadButton, ActiveItem, InactiveItem } from './Sidebar.styles'


interface SidebarProps {
  onToggle: (collapsed: boolean) => void
}

function Sidebar({ onToggle }: SidebarProps) {
  const [casinoOpen, setCasinoOpen] = useState(true)
  const [sportsOpen, setSportsOpen] = useState(false)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  const handleCasinoToggle = () => {
    setCasinoOpen(!casinoOpen)
  }

  const handleSportsToggle = () => {
    setSportsOpen(!sportsOpen)
  }

  const toggleSidebar = () => {
    const newCollapsed = !sidebarCollapsed
    setSidebarCollapsed(newCollapsed)
    onToggle(newCollapsed)
  }

  const casinoItems = [
    { text: 'Slots', icon: <SlotsIcon /> },
    { text: 'Live Casino', icon: <LiveCasinoIcon /> },
    { text: 'Game Shows', icon: <GameShowsIcon /> },
    { text: 'Casual Games', icon: <CasualGamesIcon /> },
  ]

  const sportsItems = [
    { text: 'In Play', icon: <SportsIcon /> },
    { text: 'My Bets', icon: <SportsIcon /> },
    { text: 'Favourites', icon: <SportsIcon /> },
  ]

  const otherItems = [
    { text: 'Promotions', icon: <PromotionsIcon /> },
    { text: 'Telegram Casino', icon: <TelegramIcon /> },
    { text: 'Buy Crypto', icon: <BuyCryptoIcon /> },
    { text: 'Help', icon: <HelpIcon /> },
  ]

  return (
    <StyledDrawer
      variant="permanent"
      sx={{
        width: sidebarCollapsed ? 60 : 280,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: sidebarCollapsed ? 60 : 280,
          boxSizing: 'border-box',
          transition: 'width 0.3s ease',
        },
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', p: 1 }}>
        {!sidebarCollapsed && (
          <Logo variant="h6" sx={{ flexGrow: 1 }}>
            Magenta
          </Logo>
        )}
        <CollapseButton onClick={toggleSidebar}>
          {sidebarCollapsed ? <ExpandIcon /> : <CollapseIcon />}
        </CollapseButton>
      </Box>

      {!sidebarCollapsed && (
        <>
          <Box sx={{ flexGrow: 1, overflow: 'auto' }}>
            <List>
              {/* Casino Section */}
              <ListItem disablePadding>
                <InactiveItem onClick={handleCasinoToggle}>
                  <ListItemIcon sx={{ color: '#fff' }}>
                    <CasinoIcon />
                  </ListItemIcon>
                  <ListItemText primary="Casino" />
                  {casinoOpen ? <ExpandLess /> : <ExpandMore />}
                </InactiveItem>
              </ListItem>
              <Collapse in={casinoOpen} timeout="auto" unmountOnExit>
                <List component="div" disablePadding>
                  {casinoItems.map((item, index) => (
                    <ListItem key={item.text} disablePadding>
                      <ActiveItem sx={{ pl: 4 }}>
                        <ListItemIcon sx={{ color: '#fff', minWidth: 40 }}>
                          {item.icon}
                        </ListItemIcon>
                        <ListItemText primary={item.text} />
                      </ActiveItem>
                    </ListItem>
                  ))}
                </List>
              </Collapse>

              {/* Sports Section */}
              <ListItem disablePadding>
                <InactiveItem onClick={handleSportsToggle}>
                  <ListItemIcon sx={{ color: '#fff' }}>
                    <SportsIcon />
                  </ListItemIcon>
                  <ListItemText primary="Sports" />
                  {sportsOpen ? <ExpandLess /> : <ExpandMore />}
                </InactiveItem>
              </ListItem>
              <Collapse in={sportsOpen} timeout="auto" unmountOnExit>
                <List component="div" disablePadding>
                  {sportsItems.map((item, index) => (
                    <ListItem key={item.text} disablePadding>
                      <InactiveItem sx={{ pl: 4 }}>
                        <ListItemIcon sx={{ color: '#fff', minWidth: 40 }}>
                          {item.icon}
                        </ListItemIcon>
                        <ListItemText primary={item.text} />
                      </InactiveItem>
                    </ListItem>
                  ))}
                </List>
              </Collapse>

              {/* Other Items */}
              {otherItems.map((item, index) => (
                <ListItem key={item.text} disablePadding>
                  <InactiveItem>
                    <ListItemIcon sx={{ color: '#fff' }}>
                      {item.icon}
                    </ListItemIcon>
                    <ListItemText primary={item.text} />
                  </InactiveItem>
                </ListItem>
              ))}
            </List>
          </Box>

        </>
      )}

      {sidebarCollapsed && (
        <List>
          <ListItem disablePadding>
            <InactiveItem>
              <ListItemIcon sx={{ color: '#fff', justifyContent: 'center' }}>
                <CasinoIcon />
              </ListItemIcon>
            </InactiveItem>
          </ListItem>
          <ListItem disablePadding>
            <InactiveItem>
              <ListItemIcon sx={{ color: '#fff', justifyContent: 'center' }}>
                <SportsIcon />
              </ListItemIcon>
            </InactiveItem>
          </ListItem>
        </List>
      )}
    </StyledDrawer>
  )
}

export default Sidebar
