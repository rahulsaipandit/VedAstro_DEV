import Svg, { Circle, Line, Path, Text as SvgText } from 'react-native-svg';

import { useTheme } from '@/hooks/use-theme';
import type { BirthTimeJson } from '@/lib/time';

function hourOfDay(stdTime: string): number {
  const match = /^(\d{2}):(\d{2})/.exec(stdTime);
  if (!match) return 0;
  return Number(match[1]) + Number(match[2]) / 60;
}

// 6AM at the top, clockwise - matches the reference screenshots' orientation (day on the right
// half, night on the left), unlike RadialDayClock's simpler 0h-at-top layout.
function angleForHour(hour: number): number {
  return ((hour - 6) / 24) * Math.PI * 2 - Math.PI / 2;
}

function pointOnCircle(cx: number, cy: number, r: number, angleRad: number) {
  return { x: cx + r * Math.cos(angleRad), y: cy + r * Math.sin(angleRad) };
}

function arcPath(cx: number, cy: number, r: number, startAngle: number, endAngle: number) {
  const start = pointOnCircle(cx, cy, r, startAngle);
  const end = pointOnCircle(cx, cy, r, endAngle);
  let sweepDeg = ((endAngle - startAngle) * 180) / Math.PI;
  if (sweepDeg < 0) sweepDeg += 360;
  const largeArcFlag = sweepDeg > 180 ? 1 : 0;
  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArcFlag} 1 ${end.x} ${end.y}`;
}

const HOUR_TICKS: { hour: number; label: string }[] = [
  { hour: 6, label: '6AM' },
  { hour: 9, label: '9AM' },
  { hour: 12, label: '12PM' },
  { hour: 15, label: '3PM' },
  { hour: 18, label: '6PM' },
  { hour: 21, label: '9PM' },
  { hour: 0, label: '12AM' },
  { hour: 3, label: '3AM' },
];

// The 8 classical named divisions of day and night, each a fixed 3-hour span centered between
// consecutive hour ticks above (Usha "dawn" through Triyaam "deep night"). These are traditional,
// descriptive time-of-day names, not an astronomically computed quantity like Choghadiya/Kaal -
// unlike those, getting the exact boundary wrong here doesn't misinform a religious observance,
// so a fixed 3-hour convention (rather than a verified source) is an acceptable simplification.
const PERIOD_LABELS: { midHour: number; label: string }[] = [
  { midHour: 4.5, label: 'Usha' },
  { midHour: 7.5, label: 'Purvaahna' },
  { midHour: 10.5, label: 'Madhyahna' },
  { midHour: 13.5, label: 'Aparaahna' },
  { midHour: 16.5, label: 'Saayankal' },
  { midHour: 19.5, label: 'Pradosh' },
  { midHour: 22.5, label: 'Nishith' },
  { midHour: 25.5, label: 'Triyaam' }, // wraps past midnight (1:30AM)
];

/**
 * The detailed "day clock" radial view from the reference screenshots: a 24-hour face (6AM at
 * top) with a two-tone sunrise/sunset arc, hour tick labels, the 8 classical period names around
 * the rim, and a needle for a chosen time of day. A more elaborate sibling of RadialDayClock.tsx
 * (which stays simple for the Muhurt tab list) - this is the one used by DayClock.tsx and
 * DayClockWidget.tsx.
 */
export function FullDayClock({
  sunrise,
  sunset,
  markerHour,
  size = 300,
  showLabels = true,
}: {
  sunrise: BirthTimeJson;
  sunset: BirthTimeJson;
  markerHour: number;
  size?: number;
  showLabels?: boolean;
}) {
  const theme = useTheme();
  const cx = size / 2;
  const cy = size / 2;
  const ringR = size / 2 - (showLabels ? 34 : 10);
  const strokeWidth = size * 0.045;

  const sunriseAngle = angleForHour(hourOfDay(sunrise.StdTime));
  const sunsetAngle = angleForHour(hourOfDay(sunset.StdTime));
  const markerAngle = angleForHour(markerHour);
  const markerPoint = pointOnCircle(cx, cy, ringR - strokeWidth / 2 - 4, markerAngle);

  return (
    <Svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <Circle cx={cx} cy={cy} r={ringR} fill="none" stroke={theme.backgroundSelected} strokeWidth={strokeWidth} />
      <Path
        d={arcPath(cx, cy, ringR, sunriseAngle, sunsetAngle)}
        fill="none"
        stroke="#F5C518"
        strokeWidth={strokeWidth}
        strokeLinecap="round"
      />

      {showLabels &&
        HOUR_TICKS.map(({ hour, label }) => {
          const point = pointOnCircle(cx, cy, ringR + strokeWidth / 2 + 14, angleForHour(hour));
          return (
            <SvgText key={label} x={point.x} y={point.y} fontSize={size * 0.032} fill={theme.textSecondary} textAnchor="middle" alignmentBaseline="middle">
              {label}
            </SvgText>
          );
        })}

      {showLabels &&
        PERIOD_LABELS.map(({ midHour, label }) => {
          const point = pointOnCircle(cx, cy, ringR + strokeWidth / 2 + 28, angleForHour(midHour));
          const rotationDeg = (angleForHour(midHour) * 180) / Math.PI + 90;
          return (
            <SvgText
              key={label}
              x={point.x}
              y={point.y}
              fontSize={size * 0.034}
              fill="#F5C518"
              textAnchor="middle"
              alignmentBaseline="middle"
              rotation={rotationDeg}
              origin={`${point.x}, ${point.y}`}>
              {label}
            </SvgText>
          );
        })}

      <Line x1={cx} y1={cy} x2={markerPoint.x} y2={markerPoint.y} stroke={theme.text} strokeWidth={2.5} />
      <Circle cx={cx} cy={cy} r={5} fill={theme.text} />
    </Svg>
  );
}
