/**
 * Centralized color definitions for the Magenta application
 * This file serves as the single source of truth for all colors used throughout the app
 */

export const colors = {
  // Primary brand colors
  primary: {
    main: '#6A1B9A',
    light: '#7B2CBF',
    dark: '#4A148C',
    contrastText: '#ffffff',
  },

  // Secondary colors
  secondary: {
    main: '#f44336',
    light: '#ff7961',
    dark: '#ba000d',
    contrastText: '#ffffff',
  },

  // Background colors
  background: {
    default: '#0F0F23',
    paper: '#1A1A2E',
    elevated: '#2A2A3E',
  },

  // Text colors
  text: {
    primary: '#ffffff',
    secondary: 'rgba(255, 255, 255, 0.7)',
    disabled: 'rgba(255, 255, 255, 0.5)',
    hint: 'rgba(255, 255, 255, 0.3)',
  },

  // Border colors
  border: {
    light: 'rgba(255, 255, 255, 0.1)',
    medium: 'rgba(255, 255, 255, 0.2)',
    strong: 'rgba(255, 255, 255, 0.3)',
  },

  // Status colors
  success: {
    main: '#4caf50',
    light: '#81c784',
    dark: '#388e3c',
  },

  warning: {
    main: '#ff9800',
    light: '#ffb74d',
    dark: '#f57c00',
  },

  error: {
    main: '#f44336',
    light: '#ff7961',
    dark: '#d32f2f',
  },

  info: {
    main: '#2196f3',
    light: '#64b5f6',
    dark: '#1976d2',
  },

  // Interactive states
  hover: {
    primary: '#7B2CBF',
    secondary: '#ff7961',
    background: 'rgba(255, 255, 255, 0.05)',
  },

  // Gradient colors
  gradients: {
    primary: 'linear-gradient(135deg, #6A1B9A 0%, #7B2CBF 100%)',
    background: 'linear-gradient(135deg, #0F0F23 0%, #1A1A2E 100%)',
    hero: 'linear-gradient(135deg, #182a73 0%, #218aae 69%, #20a7ac 89%)',
  },

  // Shadow colors
  shadows: {
    light: 'rgba(0, 0, 0, 0.1)',
    medium: 'rgba(0, 0, 0, 0.2)',
    strong: 'rgba(0, 0, 0, 0.3)',
  },
} as const;

// Type definitions for better TypeScript support
export type ColorPalette = typeof colors;
export type ColorKey = keyof ColorPalette;
export type ColorVariant = 'main' | 'light' | 'dark' | 'contrastText';


