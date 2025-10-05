import { MenuItem } from "@mui/material";
import { NavLink } from "react-router-dom";

interface MenuItemLinkProps {
  children: React.ReactNode;
  to: string;
}

export function MenuItemLink({ children, to }: MenuItemLinkProps) {
  return (
    <MenuItem
      component={NavLink}
      to={to}
      sx={{
        fontSize: "1.2rem",
        textTransform: "uppercase",
        fontWeight: "bold",
        color: "inherit",
        "&.active": {
          color: "yellow",
        },
      }}
    >
      {children}
    </MenuItem>
  );
}



