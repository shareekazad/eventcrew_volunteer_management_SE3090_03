import 'package:flutter/material.dart';

/// AppTheme defines the modern, sleek visual language for EventCrew Mobile.
/// Follows modern design aesthetics: vibrant indigo/violet accent, smooth surfaces,
/// refined typography, and accessible contrasts.
class AppTheme {
  static const Color primaryColor = Color(0xFF4F46E5); // Indigo 600
  static const Color primaryHover = Color(0xFF4338CA);
  static const Color primaryLight = Color(0xFFEEF2FF);
  
  static const Color secondaryColor = Color(0xFF0EA5E9); // Sky 500
  static const Color accentColor = Color(0xFF8B5CF6); // Purple 500
  
  static const Color backgroundColor = Color(0xFFF8FAFC); // Slate 50
  static const Color surfaceColor = Colors.white;
  
  static const Color textPrimary = Color(0xFF0F172A); // Slate 900
  static const Color textSecondary = Color(0xFF64748B); // Slate 500
  static const Color textMuted = Color(0xFF94A3B8); // Slate 400
  
  static const Color borderSubtle = Color(0xFFE2E8F0); // Slate 200
  static const Color borderFocus = Color(0xFF6366F1);
  
  // Status Colors
  static const Color statusSubmitted = Color(0xFFD97706); // Amber 600
  static const Color statusSubmittedBg = Color(0xFFFEF3C7);
  
  static const Color statusUnderReview = Color(0xFF2563EB); // Blue 600
  static const Color statusUnderReviewBg = Color(0xFFDBEAFE);
  
  static const Color statusApproved = Color(0xFF16A34A); // Green 600
  static const Color statusApprovedBg = Color(0xFFDCFCE7);
  
  static const Color statusRejected = Color(0xFFDC2626); // Red 600
  static const Color statusRejectedBg = Color(0xFFFEE2E2);

  static ThemeData get lightTheme {
    return ThemeData(
      useMaterial3: true,
      colorScheme: ColorScheme.fromSeed(
        seedColor: primaryColor,
        primary: primaryColor,
        secondary: secondaryColor,
        surface: surfaceColor,
        brightness: Brightness.light,
      ),
      scaffoldBackgroundColor: backgroundColor,
      fontFamily: 'Roboto',
      appBarTheme: const AppBarTheme(
        backgroundColor: Colors.white,
        foregroundColor: textPrimary,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: textPrimary,
          fontSize: 20,
          fontWeight: FontWeight.w700,
          letterSpacing: -0.5,
        ),
        iconTheme: IconThemeData(color: textPrimary),
      ),
      cardTheme: CardThemeData(
        color: surfaceColor,
        elevation: 1,
        shadowColor: Colors.black.withValues(alpha: 0.06),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: borderSubtle, width: 1),
        ),
        margin: EdgeInsets.zero,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: primaryColor,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w600,
            letterSpacing: 0.2,
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: primaryColor,
          side: const BorderSide(color: borderSubtle, width: 1.5),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 13),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: Colors.white,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: borderSubtle, width: 1),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: borderSubtle, width: 1),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: borderFocus, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: statusRejected, width: 1),
        ),
        labelStyle: const TextStyle(color: textSecondary, fontSize: 14),
        hintStyle: const TextStyle(color: textMuted, fontSize: 14),
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: Colors.white,
        indicatorColor: primaryLight,
        elevation: 8,
        labelTextStyle: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return const TextStyle(
              color: primaryColor,
              fontWeight: FontWeight.w700,
              fontSize: 12,
            );
          }
          return const TextStyle(
            color: textSecondary,
            fontWeight: FontWeight.w500,
            fontSize: 12,
          );
        }),
        iconTheme: WidgetStateProperty.resolveWith((states) {
          if (states.contains(WidgetState.selected)) {
            return const IconThemeData(color: primaryColor, size: 24);
          }
          return const IconThemeData(color: textSecondary, size: 24);
        }),
      ),
    );
  }
}
