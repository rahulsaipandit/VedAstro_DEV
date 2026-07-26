import Svg, { Circle, Path } from 'react-native-svg';

/**
 * Renders a moon-phase disc from a tithi number (1-30), using the standard two-arc "lune"
 * technique (outer arc = full disc edge, inner arc = the terminator ellipse, its radius scaling
 * from full at new/full moon down to 0 at the quarters) - conceptually the same idea as
 * github.com/Vedic-Panchanga/moon-phase-widget (not diffed line-by-line here since GitHub was
 * unreachable in this environment while this was built), computed from VedAstro's own tithi
 * rather than a separately-fetched phase value.
 */
export function MoonPhaseIcon({ tithi, size = 24 }: { tithi: number; size?: number }) {
  const elongationDeg = (tithi - 0.5) * 12; // midpoint of the tithi's 12° band
  const elongationRad = (elongationDeg * Math.PI) / 180;
  const illumination = (1 - Math.cos(elongationRad)) / 2; // 0 = new moon, 1 = full moon
  const waxing = elongationDeg < 180; // Shukla paksha (tithi 1-15) = waxing

  const r = size / 2;
  const rx = r * Math.abs(1 - illumination * 2);
  const sweep = waxing ? 1 : 0;
  const outerSweep = illumination > 0.5 ? sweep : 1 - sweep;
  const path = `M ${r} 0 A ${r} ${r} 0 0 ${sweep} ${r} ${size} A ${rx} ${r} 0 0 ${outerSweep} ${r} 0 Z`;

  return (
    <Svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
      <Circle cx={r} cy={r} r={r - 0.5} fill="none" stroke="#9AA0AC" strokeWidth={1} />
      <Path d={path} fill="#F5C518" />
    </Svg>
  );
}
