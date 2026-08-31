/** SVG path helpers ported from the design's chart logic. */

export type Pt = [number, number];

export function linePts(arr: number[], x0: number, x1: number, top: number, bottom: number, max: number): Pt[] {
  const n = arr.length;
  return arr.map((v, i) => [x0 + (i * (x1 - x0)) / (n - 1), bottom - (v / max) * (bottom - top)]);
}

export function lineD(pts: Pt[]): string {
  return pts.map((p, i) => (i ? 'L' : 'M') + p[0].toFixed(1) + ' ' + p[1].toFixed(1)).join('');
}

export function areaD(pts: Pt[], bottom: number): string {
  return (
    'M' + pts[0][0].toFixed(1) + ' ' + bottom +
    lineD(pts).replace(/^M/, 'L') +
    'L' + pts[pts.length - 1][0].toFixed(1) + ' ' + bottom + 'Z'
  );
}

export function sparkD(arr: number[], w: number, h: number): string {
  const max = Math.max(...arr);
  return lineD(linePts(arr, 2, w - 2, 3, h - 3, max));
}
