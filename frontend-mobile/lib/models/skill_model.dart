class SkillModel {
  final String id;
  final String name;
  final String category;
  final String proficiencyLevel;

  SkillModel({
    required this.id,
    required this.name,
    required this.category,
    this.proficiencyLevel = 'Intermediate',
  });

  factory SkillModel.fromJson(Map<String, dynamic> json) {
    return SkillModel(
      id: json['id']?.toString() ?? '',
      name: json['name']?.toString() ?? '',
      category: json['category']?.toString() ?? '',
      proficiencyLevel: json['proficiencyLevel']?.toString() ?? 'Intermediate',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'category': category,
      'proficiencyLevel': proficiencyLevel,
    };
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is SkillModel &&
          runtimeType == other.runtimeType &&
          id.toLowerCase() == other.id.toLowerCase();

  @override
  int get hashCode => id.toLowerCase().hashCode;
}
