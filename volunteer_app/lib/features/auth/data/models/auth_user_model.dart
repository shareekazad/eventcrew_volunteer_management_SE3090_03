/// Represents the authenticated user returned by ASP.NET Core API.
class AuthUserModel {
  final String id;
  final String fullName;
  final String email;
  final String role;

  const AuthUserModel({
    required this.id,
    required this.fullName,
    required this.email,
    required this.role,
  });

  factory AuthUserModel.fromJson(Map<String, dynamic> json) {
    String requiredString(String camelCase, String pascalCase) {
      final value = json[camelCase] ?? json[pascalCase];
      if (value is String && value.isNotEmpty) return value;
      throw FormatException(
        'Missing or invalid "$camelCase" in user response.',
      );
    }

    return AuthUserModel(
      id: requiredString('id', 'Id'),
      fullName: requiredString('fullName', 'FullName'),
      email: requiredString('email', 'Email'),
      role: requiredString('role', 'Role'),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'fullName': fullName,
    'email': email,
    'role': role,
  };
}

/// Response returned by POST /api/auth/login.
class LoginResponseModel {
  final String accessToken;
  final String expiresAt;
  final AuthUserModel user;

  const LoginResponseModel({
    required this.accessToken,
    required this.expiresAt,
    required this.user,
  });

  factory LoginResponseModel.fromJson(Map<String, dynamic> json) {
    final token = json['accessToken'] ?? json['AccessToken'];
    final expiresAt = json['expiresAt'] ?? json['ExpiresAt'];
    final rawUser = json['user'] ?? json['User'];
    if (token is! String || token.isEmpty) {
      throw const FormatException(
        'Missing or invalid "accessToken" in login response.',
      );
    }
    if (expiresAt is! String || expiresAt.isEmpty) {
      throw const FormatException(
        'Missing or invalid "expiresAt" in login response.',
      );
    }
    if (rawUser is! Map<String, dynamic>) {
      throw const FormatException(
        'Missing or invalid "user" in login response.',
      );
    }

    return LoginResponseModel(
      accessToken: token,
      expiresAt: expiresAt,
      user: AuthUserModel.fromJson(rawUser),
    );
  }
}
