/** Icônes au trait (viewBox 24×24), reprises des maquettes. */
export interface IconShape {
  paths?: string[];
  rects?: { x: number; y: number; width: number; height: number; rx: number }[];
  circles?: { cx: number; cy: number; r: number }[];
}

export const ICONS = {
  home: { paths: ['M3 10.5 12 3l9 7.5', 'M5 9.5V21h14V9.5', 'M10 21v-6h4v6'] },
  dashboard: {
    rects: [
      { x: 3, y: 3, width: 7, height: 9, rx: 1.5 },
      { x: 14, y: 3, width: 7, height: 5, rx: 1.5 },
      { x: 14, y: 12, width: 7, height: 9, rx: 1.5 },
      { x: 3, y: 16, width: 7, height: 5, rx: 1.5 },
    ],
  },
  movements: { paths: ['M7 4 3 8l4 4', 'M3 8h14', 'm17 20 4-4-4-4', 'M21 16H7'] },
  accounts: {
    paths: ['M3 7a2 2 0 0 1 2-2h13v4', 'M3 7v11a2 2 0 0 0 2 2h15V9H5a2 2 0 0 1-2-2Z'],
    circles: [{ cx: 16, cy: 14.5, r: 1.2 }],
  },
  securities: { paths: ['m12 3 9 5-9 5-9-5 9-5Z', 'm3 13 9 5 9-5'] },
  trend: { paths: ['m3 17 6-6 4 4 8-8', 'M15 7h6v6'] },
  tag: { paths: ['M3 12V4a1 1 0 0 1 1-1h8l9 9-9 9-9-9Z'], circles: [{ cx: 7.5, cy: 7.5, r: 1.5 }] },
  logout: { paths: ['M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4', 'm16 17 5-5-5-5', 'M21 12H9'] },
  user: { paths: ['M4 21c0-4 4-6 8-6s8 2 8 6'], circles: [{ cx: 12, cy: 8, r: 4 }] },
  plus: { paths: ['M12 5v14', 'M5 12h14'] },
  lock: { paths: ['M8 11V7a4 4 0 0 1 8 0v4'], rects: [{ x: 4, y: 11, width: 16, height: 10, rx: 2 }] },
  history: { paths: ['M3 12a9 9 0 1 0 3-6.7L3 8', 'M3 3v5h5', 'M12 7v5l3 2'] },
  restore: { paths: ['M3 12a9 9 0 1 0 3-6.7L3 8', 'M3 3v5h5'] },
  trash: { paths: ['M4 7h16', 'M10 11v6', 'M14 11v6', 'M6 7l1 13h10l1-13', 'M9 7V4h6v3'] },
  edit: { paths: ['M12 20h9', 'M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z'] },
  archive: {
    paths: ['M5 8v11a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1V8', 'M10 12h4'],
    rects: [{ x: 3, y: 4, width: 18, height: 4, rx: 1 }],
  },
  warning: {
    paths: ['M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z', 'M12 9v4', 'M12 17h.01'],
  },
  close: { paths: ['M18 6 6 18', 'm6 6 12 12'] },
  check: { paths: ['M20 6 9 17l-5-5'] },
  sortDown: { paths: ['M12 5v14', 'm6 13 6 6 6-6'] },
  sortUp: { paths: ['M12 19V5', 'm6 11 6-6 6 6'] },
} satisfies Record<string, IconShape>;

export type IconName = keyof typeof ICONS;
