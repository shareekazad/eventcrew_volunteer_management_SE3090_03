/// Immutable model that mirrors the backend's `AuthResponseDto`.
///
/// Holds the user's identity + the JWT issued by `/api/Auth/register` or
/// `/api/Auth/login`. Kept deliberately small — only the fields the mobile
/// app actually uses.
class AuthUser {
  final String userId;
  final String fullName;
  final String email;
  final String role;
  final String token;
  final DateTime expiresAt;

  const AuthUser({
    required this.userId,
    required this.fullName,
    required this.email,
    required this.role,
    required this.token,
    required this.expiresAt,
  });

  /// Parse from the JSON returned by the backend.
  factory AuthUser.fromJson(Map<String, dynamic> json) {
    return AuthUser(
      userId: json['userId'] as String,
      fullName: json['fullName'] as String,
      email: json['email'] as String,
      role: json['role'] as String,
      token: json['token'] as String,
      expiresAt: DateTime.parse(json['expiresAt'] as String).toLocal(),
    );
  }

  /// True if this user's token has expired.
  bool get isExpired => DateTime.now().isAfter(expiresAt);

  /// True if the user has the Volunteer role (Flutter app is volunteer-only).
  bool get isVolunteer => role.toLowerCase() == 'volunteer';
}