/**
 * Onboarding screen shown on first app launch only.
 * 3 horizontal slides using FlatList with geometric illustrations (pure RN Views).
 * Persists 'onboarding_done' flag to AsyncStorage on completion.
 */

import React, { useCallback, useRef, useState } from 'react';
import {
  Dimensions,
  FlatList,
  Pressable,
  StyleSheet,
  Text,
  View,
  type ListRenderItemInfo,
  type ViewToken,
} from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';
import { ONBOARDING_STORAGE_KEY } from '../../config/constants';
import { useAuthStore } from '../../store/authStore';

const { width: SCREEN_WIDTH } = Dimensions.get('window');

// ─── Geometric Illustrations (pure RN Views, no external images) ─────────────

function GroupIcon(): React.JSX.Element {
  return (
    <View style={iconStyles.groupContainer}>
      {/* Center person (head) */}
      <View style={[iconStyles.circle, { width: 32, height: 32, backgroundColor: colors.primary, top: 10 }]} />
      {/* Left person (head) */}
      <View style={[iconStyles.circle, { width: 24, height: 24, backgroundColor: colors.primaryLight, top: 20, left: 10 }]} />
      {/* Right person (head) */}
      <View style={[iconStyles.circle, { width: 24, height: 24, backgroundColor: colors.primaryLight, top: 20, right: 10 }]} />
      {/* Center body */}
      <View style={[iconStyles.body, { backgroundColor: colors.primary, width: 50, bottom: 10 }]} />
      {/* Left body */}
      <View style={[iconStyles.body, { backgroundColor: colors.primaryLight, width: 30, bottom: 10, left: 5 }]} />
      {/* Right body */}
      <View style={[iconStyles.body, { backgroundColor: colors.primaryLight, width: 30, bottom: 10, right: 5 }]} />
    </View>
  );
}

function PaymentIcon(): React.JSX.Element {
  return (
    <View style={iconStyles.paymentContainer}>
      {/* Card */}
      <View style={iconStyles.card}>
        {/* Card stripe */}
        <View style={iconStyles.cardStripe} />
        {/* Card circles (payment network logo) */}
        <View style={iconStyles.cardChipRow}>
          <View style={[iconStyles.circle, { width: 16, height: 16, backgroundColor: colors.secondary }]} />
          <View style={[iconStyles.circle, { width: 16, height: 16, backgroundColor: colors.secondaryLight, marginLeft: -6 }]} />
        </View>
      </View>
      {/* Shield / check mark */}
      <View style={iconStyles.shield}>
        <Text style={iconStyles.shieldCheck}>✓</Text>
      </View>
    </View>
  );
}

function ChartIcon(): React.JSX.Element {
  return (
    <View style={iconStyles.chartContainer}>
      {/* Bars */}
      <View style={[iconStyles.bar, { height: 30, backgroundColor: colors.primaryLight }]} />
      <View style={[iconStyles.bar, { height: 50, backgroundColor: colors.primary }]} />
      <View style={[iconStyles.bar, { height: 65, backgroundColor: colors.primaryLight }]} />
      <View style={[iconStyles.bar, { height: 80, backgroundColor: colors.primary }]} />
      {/* Trend dots */}
      <View style={iconStyles.trendLine}>
        <View style={[iconStyles.trendDot, { bottom: 25 }]} />
        <View style={[iconStyles.trendDot, { bottom: 45 }]} />
        <View style={[iconStyles.trendDot, { bottom: 60 }]} />
        <View style={[iconStyles.trendDot, { bottom: 75 }]} />
      </View>
    </View>
  );
}

const iconStyles = StyleSheet.create({
  groupContainer: {
    width: 100,
    height: 100,
    alignItems: 'center',
    justifyContent: 'center',
    position: 'relative',
  },
  circle: {
    borderRadius: 9999,
    position: 'absolute',
  },
  body: {
    height: 28,
    borderTopLeftRadius: 14,
    borderTopRightRadius: 14,
    position: 'absolute',
  },
  paymentContainer: {
    width: 100,
    height: 100,
    alignItems: 'center',
    justifyContent: 'center',
  },
  card: {
    width: 90,
    height: 56,
    backgroundColor: colors.primary,
    borderRadius: 10,
    overflow: 'hidden',
  },
  cardStripe: {
    height: 12,
    backgroundColor: colors.primaryDark,
    marginTop: 14,
  },
  cardChipRow: {
    flexDirection: 'row',
    position: 'absolute',
    bottom: 8,
    right: 10,
  },
  shield: {
    width: 28,
    height: 28,
    borderRadius: 14,
    backgroundColor: colors.secondary,
    alignItems: 'center',
    justifyContent: 'center',
    position: 'absolute',
    bottom: 4,
    right: 4,
  },
  shieldCheck: {
    color: colors.textOnPrimary,
    fontSize: 16,
    fontWeight: '700',
  },
  chartContainer: {
    width: 100,
    height: 100,
    flexDirection: 'row',
    alignItems: 'flex-end',
    justifyContent: 'center',
    gap: 6,
    position: 'relative',
  },
  bar: {
    width: 18,
    borderRadius: 4,
  },
  trendLine: {
    ...StyleSheet.absoluteFillObject,
    flexDirection: 'row',
    justifyContent: 'space-around',
    alignItems: 'flex-end',
  },
  trendDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: colors.secondary,
    position: 'absolute',
  },
});

// ─── Slide Data ──────────────────────────────────────────────────────────────

interface Slide {
  id: string;
  title: string;
  description: string;
  illustration: React.JSX.Element;
}

const slides: Slide[] = [
  {
    id: '1',
    title: 'Épargnez ensemble',
    description:
      'Rejoignez une tontine et épargnez en groupe avec confiance. ' +
      'Un système communautaire qui valorise la solidarité et la discipline financière.',
    illustration: <GroupIcon />,
  },
  {
    id: '2',
    title: 'Paiements sécurisés',
    description:
      'Payez via Orange Money en toute sécurité. ' +
      'Chaque transaction est tracée et vérifiable grâce à notre piste d\u2019audit complète.',
    illustration: <PaymentIcon />,
  },
  {
    id: '3',
    title: 'Suivi transparent',
    description:
      'Suivez votre score de crédit et votre historique en temps réel. ' +
      'Une visibilité totale sur vos contributions et votre progression.',
    illustration: <ChartIcon />,
  },
];

// ─── Component ───────────────────────────────────────────────────────────────

export function OnboardingScreen(): React.JSX.Element {
  const [activeIndex, setActiveIndex] = useState(0);
  const flatListRef = useRef<FlatList<Slide>>(null);
  const setHasSeenOnboarding = useAuthStore((s) => s.setHasSeenOnboarding);

  const isLastSlide = activeIndex === slides.length - 1;

  const completeOnboarding = useCallback(async () => {
    try {
      await AsyncStorage.setItem(ONBOARDING_STORAGE_KEY, 'true');
    } catch {
      // If AsyncStorage fails, silently continue — onboarding should never block
    }
    setHasSeenOnboarding(true);
  }, [setHasSeenOnboarding]);

  const handleNext = useCallback(() => {
    if (isLastSlide) {
      void completeOnboarding();
    } else {
      flatListRef.current?.scrollToIndex({ index: activeIndex + 1, animated: true });
    }
  }, [activeIndex, isLastSlide, completeOnboarding]);

  const handleSkip = useCallback(() => {
    void completeOnboarding();
  }, [completeOnboarding]);

  const onViewableItemsChanged = useRef(
    ({ viewableItems }: { viewableItems: ViewToken[] }) => {
      if (viewableItems.length > 0 && viewableItems[0].index != null) {
        setActiveIndex(viewableItems[0].index);
      }
    },
  ).current;

  const viewabilityConfig = useRef({ viewAreaCoveragePercentThreshold: 50 }).current;

  const renderSlide = useCallback(
    ({ item }: ListRenderItemInfo<Slide>) => (
      <View style={styles.slide}>
        <View style={styles.illustrationContainer}>{item.illustration}</View>
        <Text style={styles.title}>{item.title}</Text>
        <Text style={styles.description}>{item.description}</Text>
      </View>
    ),
    [],
  );

  const keyExtractor = useCallback((item: Slide) => item.id, []);

  return (
    <View style={styles.container}>
      {/* Skip button */}
      <Pressable
        style={styles.skipButton}
        onPress={handleSkip}
        accessibilityRole="button"
        accessibilityLabel="Passer l'introduction"
      >
        <Text style={styles.skipText}>Passer</Text>
      </Pressable>

      {/* Slides */}
      <FlatList
        ref={flatListRef}
        data={slides}
        renderItem={renderSlide}
        keyExtractor={keyExtractor}
        horizontal
        pagingEnabled
        showsHorizontalScrollIndicator={false}
        bounces={false}
        onViewableItemsChanged={onViewableItemsChanged}
        viewabilityConfig={viewabilityConfig}
        getItemLayout={(_data, index) => ({
          length: SCREEN_WIDTH,
          offset: SCREEN_WIDTH * index,
          index,
        })}
      />

      {/* Bottom section: dots + button */}
      <View style={styles.bottomSection}>
        {/* Dot indicators */}
        <View style={styles.dotsContainer}>
          {slides.map((slide, index) => (
            <View
              key={slide.id}
              style={[
                styles.dot,
                index === activeIndex ? styles.dotActive : styles.dotInactive,
              ]}
            />
          ))}
        </View>

        {/* Next / Commencer button */}
        <Pressable
          style={styles.nextButton}
          onPress={handleNext}
          accessibilityRole="button"
          accessibilityLabel={isLastSlide ? 'Commencer' : 'Suivant'}
        >
          <Text style={styles.nextButtonText}>
            {isLastSlide ? 'Commencer' : 'Suivant'}
          </Text>
        </Pressable>
      </View>
    </View>
  );
}

// ─── Styles ──────────────────────────────────────────────────────────────────

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.surface,
  },
  skipButton: {
    position: 'absolute',
    top: spacing.xxl,
    right: spacing.lg,
    zIndex: 1,
    paddingVertical: spacing.sm,
    paddingHorizontal: spacing.md,
  },
  skipText: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  slide: {
    width: SCREEN_WIDTH,
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.xl,
  },
  illustrationContainer: {
    marginBottom: spacing.xl,
    alignItems: 'center',
    justifyContent: 'center',
    width: 160,
    height: 160,
    borderRadius: borderRadius.xl,
    backgroundColor: colors.background,
  },
  title: {
    fontSize: fontSizes.xxl,
    fontWeight: '700',
    color: colors.textPrimary,
    textAlign: 'center',
    marginBottom: spacing.md,
  },
  description: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    textAlign: 'center',
    lineHeight: 24,
    paddingHorizontal: spacing.md,
  },
  bottomSection: {
    paddingBottom: spacing.xxl,
    paddingHorizontal: spacing.xl,
    alignItems: 'center',
  },
  dotsContainer: {
    flexDirection: 'row',
    marginBottom: spacing.lg,
  },
  dot: {
    width: 10,
    height: 10,
    borderRadius: borderRadius.full,
    marginHorizontal: spacing.xs,
  },
  dotActive: {
    backgroundColor: colors.primary,
    width: 24,
  },
  dotInactive: {
    backgroundColor: colors.disabled,
  },
  nextButton: {
    backgroundColor: colors.primary,
    paddingVertical: spacing.md,
    paddingHorizontal: spacing.xxl,
    borderRadius: borderRadius.lg,
    width: '100%',
    alignItems: 'center',
  },
  nextButtonText: {
    color: colors.textOnPrimary,
    fontSize: fontSizes.lg,
    fontWeight: '600',
  },
});
