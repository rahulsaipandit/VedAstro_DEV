import Svg, { Circle, Line, Path } from 'react-native-svg';

import { useTheme } from '@/hooks/use-theme';
import type { BirthTimeJson } from '@/lib/time';

function hourOfDay(stdTime: string): number {
  const match = /^(\d{2}):(\d{2})/.exec(stdTime);
  if (!match) return 0;
  return Number(match[1]) + Number(match[2]) / 60;
}

// 0h at the top, clockwise - a plain 24-hour clock face, not the reference app's compass-style
// Usha/Pradosh/Nishith labels (see designHinduCalendar.md's "Visual design" note: layout
// reference only, not a style target).
function angleForHour(hour: number): number {
  return (hour / 24) * Math.PI * 2 - Math.PI / 2;
}

function pointOnCircle(cx: number, cy: number, r: number, angle: number) {
  return { x: cx + r * Math.cos(angle), y: cy + r * Math.sin(angle) };
}

function arcPath(cx: number, cy: number, r: number, startAngle: number, endAngle: number) {
  const start = pointOnCircle(cx, cy, r, startAngle);
  const end = pointOnCircle(cx, cy, r, endAngle);
  let sweepDeg = ((endAngle - startAngle) * 180) / Math.PI;
  if (sweepDeg < 0) sweepDeg += 360;
  const largeArcFlag = sweepDeg > 180 ? 1 : 0;
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArcFlag} 1 ${end.x} ${end.y}`;
}

/**
 * A day's sunrise-to-sunset arc on a 24-hour clock face, with a marker for a given time of day -
 * the radial "day clock" view from the reference screenshots, reduced to its actual information
 * content (day/night split + where a chosen instant falls) rather than every label crammed
 * around the reference app's compass rim.
 */
export function RadialDayClock({
  sunrise,
  sunset,
  markerHour,
  size = 200,
}: {
  sunrise: BirthTimeJson;
  sunset: BirthTimeJson;
  markerHour: number;
  size?: number;
}) {
  const theme = useTheme();
  const strokeWidth = 14;
  const r = size / 2 - strokeWidth;
  const cx = size / 2;
  const cy = size / 2;

  const sunriseAngle = angleForHour(hourOfDay(sunrise.StdTime));
  const sunsetAngle = angleForHour(hourOfDay(sunset.StdTime));
  const markerAngle = angleForHour(markerHour);
  const markerPoint = pointOnCircle(cx, cy, r, markerAngle);

  return (
    <Svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <Circle cx={cx} cy={cy} r={r} fill="none" stroke={theme.backgroundSelected} strokeWidth={strokeWidth} />
      <Path
        d={arcPath(cx, cy, r, sunriseAngle, sunsetAngle)}
        fill="none"
        stroke="#F5C518"
        strokeWidth={strokeWidth}
        strokeLinecap="round"
      />
      <Line x1={cx} y1={cy} x2={markerPoint.x} y2={markerPoint.y} stroke={theme.text} strokeWidth={2} />
      <Circle cx={cx} cy={cy} r={5} fill={theme.text} />
    </Svg>
  );
}
