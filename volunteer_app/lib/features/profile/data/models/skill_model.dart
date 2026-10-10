/// Dart model mirroring the backend's `SkillResponseDto`.
///
/// Represents a skill from the catalog (not yet attached to a volunteer).
class SkillModel {
  final String id;
  final String name;
  final String category;
  final String? description;

  const SkillModel({
    required this.id,
    required this.name,
    required this.category,
    this.description,
  });

  factory SkillModel.fromJson(Map<String, dynamic> json) {
    return SkillModel(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      category: json['category'] as String? ?? '',
      description: json['description'] as String?,
    );
  }
}