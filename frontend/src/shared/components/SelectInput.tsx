import { FormControl, FormHelperText, InputLabel, MenuItem, Select } from "@mui/material";

interface SelectInputProps {
    items: {text: string, value: string}[];
    label: string;
    value: string;
    onChange: (value: string) => void;
    error?: string;
    helperText?: string;
}

export default function SelectInput({ 
    items, 
    label, 
    value, 
    onChange, 
    error, 
    helperText 
}: SelectInputProps) {
  return (
    <FormControl fullWidth error={!!error}>
        <InputLabel>{label}</InputLabel>
        <Select
            value={value || ""}
            label={label}
            onChange={(e) => onChange(e.target.value)}>
            {items.map(item => (
                <MenuItem key={item.value} value={item.value}> {item.text}</MenuItem>
            ))}
        </Select>
        <FormHelperText>{error || helperText}</FormHelperText>
    </FormControl>
  );
}
