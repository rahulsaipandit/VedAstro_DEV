import { Image, ScrollView, StyleSheet, View } from 'react-native';

import { ThemedText } from '@/components/themed-text';
import { ThemedView } from '@/components/themed-view';
import { BODY_TYPE_ROWS } from '@/constants/bodyTypes';
import { MaxContentWidth, Spacing } from '@/constants/theme';

// require() needs a static, literal path per call - can't loop a dynamic filename string, so
// every image referenced by src/constants/bodyTypes.ts gets an explicit entry here.
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const PERSON_IMAGES: Record<string, any> = {
  'JeremyIrons.jpg': require('@/assets/images/person/JeremyIrons.jpg'),
  'PeterStormare.jpg': require('@/assets/images/person/PeterStormare.jpg'),
  'JonVoight.webp': require('@/assets/images/person/JonVoight.webp'),
  'SallyField.jpg': require('@/assets/images/person/SallyField.jpg'),
  'HelenHunt.jpg': require('@/assets/images/person/HelenHunt.jpg'),
  'AngelinaJolie.webp': require('@/assets/images/person/AngelinaJolie.webp'),
  'DenzelWashington.jpg': require('@/assets/images/person/DenzelWashington.jpg'),
  'EthanHawke.webp': require('@/assets/images/person/EthanHawke.webp'),
  'FrançoisTruffaut.jpg': require('@/assets/images/person/FrançoisTruffaut.jpg'),
  'JessicaLange.webp': require('@/assets/images/person/JessicaLange.webp'),
  'DianeKeaton.webp': require('@/assets/images/person/DianeKeaton.webp'),
  'HelenMirren.jpg': require('@/assets/images/person/HelenMirren.jpg'),
  'JamieLeeCurtis.jpg': require('@/assets/images/person/JamieLeeCurtis.jpg'),
  'WhitneyHouston.jpg': require('@/assets/images/person/WhitneyHouston.jpg'),
  'IngridBergman.jpg': require('@/assets/images/person/IngridBergman.jpg'),
};

function ImageRow({ filenames }: { filenames: string[] }) {
  return (
    <View style={styles.imageRow}>
      {filenames.map((filename, index) => (
        <Image key={`${filename}-${index}`} source={PERSON_IMAGES[filename]} style={styles.personImage} />
      ))}
    </View>
  );
}

/**
 * Port of Website/Pages/BodyTypes.razor. Note on data fidelity (found while extracting, not
 * introduced here): only the first two rows (Horse Male/Female) have unique example photos in
 * the original — every other row's male-example list is literally "JeremyIrons.jpg" repeated
 * three times, and most female-example lists repeat the same three names. That's a copy-paste
 * artifact already present in the source page, preserved as-is rather than "fixed" with invented
 * examples.
 */
export default function BodyTypesScreen() {
  return (
    <ScrollView contentContainerStyle={styles.scrollContent}>
      <ThemedView style={styles.page}>
        <ThemedText type="title">Body Types (Yoni)</ThemedText>
        <ThemedText themeColor="textSecondary" style={styles.subtitle}>
          Catalog of 27 astro Vedic body types with known examples, to document and aid in
          learning to recognize on your own.
        </ThemedText>

        <ThemedView style={styles.table}>
          {BODY_TYPE_ROWS.map((row) => (
            <ThemedView key={row.label} style={styles.row} type="backgroundElement">
              <ThemedText type="smallBold" style={styles.label}>
                {row.label}
              </ThemedText>
              <ThemedView style={styles.examplesCol}>
                <ThemedText type="small" themeColor="textSecondary">
                  Male
                </ThemedText>
                <ImageRow filenames={row.maleImages} />
              </ThemedView>
              <ThemedView style={styles.examplesCol}>
                <ThemedText type="small" themeColor="textSecondary">
                  Female
                </ThemedText>
                <ImageRow filenames={row.femaleImages} />
              </ThemedView>
            </ThemedView>
          ))}
        </ThemedView>
      </ThemedView>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  scrollContent: {
    alignItems: 'center',
  },
  page: {
    width: '100%',
    maxWidth: MaxContentWidth,
    paddingHorizontal: Spacing.three,
    paddingTop: Spacing.five,
    paddingBottom: Spacing.six,
    gap: Spacing.four,
  },
  subtitle: {
    marginBottom: Spacing.one,
  },
  table: {
    gap: Spacing.three,
  },
  row: {
    borderRadius: 12,
    padding: Spacing.three,
    gap: Spacing.two,
  },
  label: {
    marginBottom: Spacing.one,
  },
  examplesCol: {
    gap: Spacing.one,
  },
  imageRow: {
    flexDirection: 'row',
    gap: Spacing.one,
  },
  personImage: {
    width: 64,
    height: 64,
    borderRadius: 8,
  },
});
