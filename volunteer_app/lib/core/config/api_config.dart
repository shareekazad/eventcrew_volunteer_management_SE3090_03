import 'package:flutter/foundation.dart';

/// Configuration for the EventCrew API.
///
/// The base URL differs between platforms during development:
/// - Android emulator: 10.0.2.2 (host's localhost)
/// - Web (Chrome):     localhost (same host)
/// - iOS simulator:    localhost
/// - Physical devices: set EVENTCREW_API_BASE_URL to the development machine's
///   LAN URL and configure the API to listen on that network interface. Use
///   HTTPS on iOS physical devices; Android debug builds allow local HTTP.
///
/// In production (deployed), this will be the Render URL.
class ApiConfig {
  ApiConfig._(); // prevent instantiation

  /// Set to true to use the production API URL.
  /// Set to false to use the local dev API URL.
  static const bool useProduction = false;

  /// Override with `--dart-define=EVENTCREW_API_BASE_URL=<api-url>`.
  static const String developmentOverride = String.fromEnvironment(
    'EVENTCREW_API_BASE_URL',
  );

  /// Development base URL (ASP.NET Core running locally).
  static const String devBaseUrl = 'http://localhost:5100';
  static const String androidEmulatorBaseUrl = 'http://10.0.2.2:5100';

  /// Production base URL (to be filled after deployment).
  static const String prodBaseUrl = 'https://eventcrew-api.onrender.com';

  /// The active base URL.
  static String get baseUrl {
    if (useProduction) return prodBaseUrl;
    if (developmentOverride.isNotEmpty) {
      return developmentOverride.replaceFirst(RegExp(r'/+$'), '');
    }
    if (!kIsWeb && defaultTargetPlatform == TargetPlatform.android) {
      return androidEmulatorBaseUrl;
    }
    return devBaseUrl;
  }

  /// Timeout for HTTP requests.
  static const Duration requestTimeout = Duration(seconds: 30);
}
