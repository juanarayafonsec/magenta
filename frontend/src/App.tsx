import { useState } from 'react'
import { Box } from '@mui/material'
import Navbar from './components/Navbar'
import Sidebar from './components/Sidebar'
import reactLogo from './assets/react.svg'
import viteLogo from '/vite.svg'
import './App.css'

function App() {
  const [count, setCount] = useState(0)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false)

  return (
    <Box sx={{ 
      backgroundColor: '#0F0F23', 
      minHeight: '100vh',
      width: '100vw',
      margin: 0,
      padding: 0,
      overflowX: 'hidden',
      position: 'relative'
    }}>
      <Sidebar onToggle={setSidebarCollapsed} />
      <Navbar sidebarCollapsed={sidebarCollapsed} />
      <Box sx={{ 
        marginLeft: sidebarCollapsed ? '60px' : '280px',
        marginTop: '64px',
        minHeight: 'calc(100vh - 64px)',
        position: 'relative',
        transition: 'margin-left 0.3s ease',
        backgroundColor: '#0F0F23',
        width: sidebarCollapsed ? 'calc(100% - 60px)' : 'calc(100% - 280px)'
      }}>
        <Box sx={{ 
          p: { xs: 2, sm: 4 },
          width: '100%',
          maxWidth: '100%',
          margin: 0,
          overflowX: 'hidden',
          flexGrow: 1,
          backgroundColor: '#0F0F23'
        }}>
          <div>
            <a href="https://vite.dev" target="_blank">
              <img src={viteLogo} className="logo" alt="Vite logo" />
            </a>
            <a href="https://react.dev" target="_blank">
              <img src={reactLogo} className="logo react" alt="React logo" />
            </a>
          </div>
          <h1 style={{ color: '#fff' }}>Vite + React</h1>
          <div className="card">
            <button onClick={() => setCount((count) => count + 1)}>
              count is {count}
            </button>
            <p style={{ color: '#fff' }}>
              Edit <code>src/App.tsx</code> and save to test HMR
            </p>
          </div>
          <p className="read-the-docs" style={{ color: '#fff' }}>
            Click on the Vite and React logos to learn more
          </p>
        </Box>
      </Box>
    </Box>
  )
}

export default App
