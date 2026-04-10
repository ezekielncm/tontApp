/**
 * Notifications Screen.
 * Displays a list of in-app notifications for the authenticated user.
 * Reads from local notification store (populated via push/expo-notifications).
 */

import React, { useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  TouchableOpacity,
} from 'react-native';
import type { NativeStackScreenProps } from '@react-navigation/native-stack';
import type { AppStackParamList } from '../../navigation/types';
import { colors, spacing, fontSizes, borderRadius } from '../../config/theme';
import { useNotificationStore } from '../../store/notificationStore';
import type { NotificationItem } from '../../types/api';

type Props = NativeStackScreenProps<AppStackParamList, 'Notifications'>;

const typeConfig: Record<string, { icon: string; color: string }> = {
  ConfirmationPaiement: { icon: '✅', color: '#1B5E20' },
  RappelPaiement: { icon: '⏰', color: '#E65100' },
  OuvertureTour: { icon: '🔄', color: '#0D47A1' },
  Suspension: { icon: '⛔', color: '#B71C1C' },
  MessagePersonnalise: { icon: '💬', color: '#4A148C' },
  RelanceSms: { icon: '📱', color: '#FF6F00' },
};

function formatNotifDate(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  const diffHours = Math.floor(diffMin / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffMin < 1) return "À l'instant";
  if (diffMin < 60) return `Il y a ${diffMin} min`;
  if (diffHours < 24) return `Il y a ${diffHours}h`;
  if (diffDays < 7) return `Il y a ${diffDays}j`;
  return date.toLocaleDateString('fr-FR', {
    day: 'numeric',
    month: 'short',
  });
}

export function NotificationsScreen(_props: Props): React.JSX.Element {
  const notifications = useNotificationStore((s) => s.notifications);
  const markAsRead = useNotificationStore((s) => s.markAsRead);
  const clearAll = useNotificationStore((s) => s.clearAll);

  const handlePress = useCallback(
    (id: string) => {
      markAsRead(id);
    },
    [markAsRead],
  );

  const renderItem = useCallback(
    ({ item }: { item: NotificationItem }) => {
      const config = typeConfig[item.type] ?? { icon: '🔔', color: colors.textPrimary };

      return (
        <TouchableOpacity
          style={[styles.card, !item.lu && styles.cardUnread]}
          onPress={() => handlePress(item.id)}
          activeOpacity={0.7}
        >
          <View style={styles.iconContainer}>
            <Text style={styles.icon}>{config.icon}</Text>
          </View>
          <View style={styles.cardContent}>
            <View style={styles.cardHeader}>
              <Text
                style={[
                  styles.titre,
                  !item.lu && styles.titreUnread,
                ]}
                numberOfLines={1}
              >
                {item.titre}
              </Text>
              <Text style={styles.date}>
                {formatNotifDate(item.dateCreation)}
              </Text>
            </View>
            <Text
              style={styles.message}
              numberOfLines={2}
            >
              {item.message}
            </Text>
          </View>
          {!item.lu && <View style={styles.unreadDot} />}
        </TouchableOpacity>
      );
    },
    [handlePress],
  );

  const renderEmpty = useCallback(
    () => (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyEmoji}>🔔</Text>
        <Text style={styles.emptyTitle}>Aucune notification</Text>
        <Text style={styles.emptySubtitle}>
          Vous recevrez ici les notifications de paiements, rappels et
          mises à jour de vos tontines.
        </Text>
      </View>
    ),
    [],
  );

  const unreadCount = notifications.filter((n) => !n.lu).length;

  return (
    <View style={styles.container}>
      {notifications.length > 0 && (
        <View style={styles.headerBar}>
          <Text style={styles.headerText}>
            {unreadCount > 0
              ? `${unreadCount} non lue${unreadCount > 1 ? 's' : ''}`
              : 'Toutes lues'}
          </Text>
          <TouchableOpacity onPress={clearAll}>
            <Text style={styles.clearText}>Tout effacer</Text>
          </TouchableOpacity>
        </View>
      )}

      <FlatList
        data={notifications}
        keyExtractor={(item) => item.id}
        renderItem={renderItem}
        ListEmptyComponent={renderEmpty}
        contentContainerStyle={styles.listContent}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  headerBar: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.sm,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerText: {
    fontSize: fontSizes.sm,
    fontWeight: '600',
    color: colors.textSecondary,
  },
  clearText: {
    fontSize: fontSizes.sm,
    color: colors.error,
    fontWeight: '600',
  },
  listContent: {
    padding: spacing.md,
    paddingBottom: spacing.xxl,
    flexGrow: 1,
  },
  card: {
    flexDirection: 'row',
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    padding: spacing.md,
    marginBottom: spacing.sm,
    alignItems: 'flex-start',
  },
  cardUnread: {
    backgroundColor: colors.primaryLight,
    borderLeftWidth: 3,
    borderLeftColor: colors.primary,
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.background,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: spacing.sm,
  },
  icon: {
    fontSize: 20,
  },
  cardContent: {
    flex: 1,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  titre: {
    fontSize: fontSizes.md,
    color: colors.textPrimary,
    flex: 1,
    marginRight: spacing.sm,
  },
  titreUnread: {
    fontWeight: '700',
  },
  date: {
    fontSize: fontSizes.xs,
    color: colors.textSecondary,
  },
  message: {
    fontSize: fontSizes.sm,
    color: colors.textSecondary,
    lineHeight: 20,
  },
  unreadDot: {
    width: 10,
    height: 10,
    borderRadius: 5,
    backgroundColor: colors.primary,
    marginLeft: spacing.xs,
    marginTop: spacing.xs,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: spacing.xxl * 2,
  },
  emptyEmoji: {
    fontSize: 48,
    marginBottom: spacing.md,
  },
  emptyTitle: {
    fontSize: fontSizes.lg,
    fontWeight: '700',
    color: colors.textPrimary,
    marginBottom: spacing.xs,
  },
  emptySubtitle: {
    fontSize: fontSizes.md,
    color: colors.textSecondary,
    textAlign: 'center',
    paddingHorizontal: spacing.lg,
  },
});
