/// Represents a role requirement nested inside an event.
///
/// Mirrors the ASP.NET Core RoleRequirementDto shape.
class RoleRequirementModel {
  final String id;
  final String roleName;
  final String? description;
  final int requiredHeadcount;
  final String minExperienceLevel;

  const RoleRequirementModel({
    required this.id,
    required this.roleName,
    required this.description,
    required this.requiredHeadcount,
    required this.minExperienceLevel,
  });

  /// Constructs a RoleRequirementModel from the JSON returned by the API.
  factory RoleRequirementModel.fromJson(Map<String, dynamic> json) {
    return RoleRequirementModel(
      id: json['id'] as String,
      roleName: json['roleName'] as String,
      description: json['description'] as String?,
      requiredHeadcount: json['requiredHeadcount'] as int,
      minExperienceLevel: json['minExperienceLevel'] as String,
    );
  }

  /// A short label for UI, e.g. "Usher × 5".
  String get displayLabel => '$roleName × $requiredHeadcount';
}