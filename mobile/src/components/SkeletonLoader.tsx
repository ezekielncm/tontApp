/**
 * Skeleton loader for perceived performance on 3G.
 * Renders placeholder shimmer blocks while data loads.
 */

import React, { useEffect, useRef } from 'react';
import { View, Animated, StyleSheet, type ViewStyle } from 'react-native';
import { colors, borderRadius, spacing } from '../config/theme';

interface SkeletonLoaderProps {
  /** Width of the skeleton block */
  width: number | `${number}%`;
  /** Height of the skeleton block */
  height: number;
  /** Border radius override */
  radius?: number;
  /** Optional style overrides */
  style?: ViewStyle;
}

export function SkeletonLoader({
  width,
  height,
  radius = borderRadius.md,
  style,
}: SkeletonLoaderProps): React.JSX.Element {
  const opacity = useRef(new Animated.Value(0.3)).current;

  useEffect(() => {
    const animation = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, {
          toValue: 0.7,
          duration: 800,
          useNativeDriver: true,
        }),
        Animated.timing(opacity, {
          toValue: 0.3,
          duration: 800,
          useNativeDriver: true,
        }),
      ]),
    );
    animation.start();
    return () => animation.stop();
  }, [opacity]);

  return (
    <Animated.View
      style={[
        styles.skeleton,
        { width, height, borderRadius: radius, opacity },
        style,
      ]}
      accessibilityLabel="Chargement en cours"
      accessibilityRole="progressbar"
    />
  );
}

/** Skeleton placeholder for a TontineCard */
export function TontineCardSkeleton(): React.JSX.Element {
  return (
    <View style={styles.cardSkeleton}>
      <View style={styles.cardSkeletonHeader}>
        <SkeletonLoader width="60%" height={20} />
        <SkeletonLoader width={60} height={20} radius={borderRadius.sm} />
      </View>
      <SkeletonLoader width="40%" height={16} style={styles.skeletonLine} />
      <SkeletonLoader width="30%" height={14} style={styles.skeletonLine} />
    </View>
  );
}

/** Skeleton list for HomeScreen */
export function HomeScreenSkeleton(): React.JSX.Element {
  return (
    <View style={styles.listSkeleton}>
      <TontineCardSkeleton />
      <TontineCardSkeleton />
      <TontineCardSkeleton />
    </View>
  );
}

/** Skeleton for TontineDetailScreen */
export function TontineDetailSkeleton(): React.JSX.Element {
  return (
    <View style={styles.detailSkeleton}>
      <SkeletonLoader width="70%" height={28} />
      <SkeletonLoader width="100%" height={16} style={styles.skeletonLine} />
      <SkeletonLoader width="100%" height={12} style={styles.skeletonBar} radius={borderRadius.full} />
      <View style={styles.skeletonSection}>
        <SkeletonLoader width="50%" height={20} />
        <SkeletonLoader width="100%" height={48} style={styles.skeletonLine} />
        <SkeletonLoader width="100%" height={48} style={styles.skeletonLine} />
        <SkeletonLoader width="100%" height={48} style={styles.skeletonLine} />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  skeleton: {
    backgroundColor: colors.border,
  },
  cardSkeleton: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.lg,
    padding: spacing.md,
    marginBottom: spacing.md,
    borderWidth: 1,
    borderColor: colors.border,
  },
  cardSkeletonHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: spacing.sm,
  },
  skeletonLine: {
    marginTop: spacing.sm,
  },
  skeletonBar: {
    marginTop: spacing.md,
  },
  listSkeleton: {
    paddingHorizontal: spacing.lg,
    paddingTop: spacing.md,
  },
  detailSkeleton: {
    padding: spacing.lg,
  },
  skeletonSection: {
    marginTop: spacing.lg,
  },
});
