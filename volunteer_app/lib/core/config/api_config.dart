/// Configuration for the EventCrew API.
///
/// The base URL differs between platforms during development:
/// - Android emulator: 10.0.2.2 (host's localhost)
/// - Web (Chrome):     localhost (same host)
/// - iOS simulator:    localhost
///
/// In production (deployed), this will be the Render URL.
class ApiConfig {
  ApiConfig._(); // prevent instantiation

  /// Set to true to use the production API URL.
  /// Set to false to use the local dev API URL.
  static const bool useProduction = true;

  /// Development base URL (ASP.NET Core running locally).
  static const String devBaseUrl = 'http://localhost:5100';

  /// Production base URL (to be filled after deployment).
  static const String prodBaseUrl = 'https://eventcrewvolunteermanagementse309003-production.up.railway.app';
  /// The active base URL.
  static String get baseUrl => useProduction ? prodBaseUrl : devBaseUrl;

  /// Timeout for HTTP requests.
  static const Duration requestTimeout = Duration(seconds: 30);
}