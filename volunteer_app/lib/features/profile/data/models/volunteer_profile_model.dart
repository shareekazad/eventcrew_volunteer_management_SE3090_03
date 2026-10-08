/// Dart model mirroring `VolunteerProfileResponseDto`.
class VolunteerProfileModel {
  final String id;
  final String userId;
  final String? fullName;
  final String? email;
  final String emergencyContact;
  final String? bio;
  final int maxHoursPerWeek;
  final double ratingScore;
  final List<ProfileSkill> skills;

  const VolunteerProfileModel({
    required this.id,
    required this.userId,
    this.fullName,
    this.email,
    required this.emergencyContact,
    this.bio,
    required this.maxHoursPerWeek,
    required this.ratingScore,
    required this.skills,
  });

  factory VolunteerProfileModel.fromJson(Map<String, dynamic> json) {
    final rawSkills = json['skills'] as List<dynamic>? ?? const [];
    return VolunteerProfileModel(
      id: json['id'] as String,
      userId: json['userId'] as String,
      fullName: json['fullName'] as String?,
      email: json['email'] as String?,
      emergencyContact: json['emergencyContact'] as String? ?? '',
      bio: json['bio'] as String?,
      maxHoursPerWeek: (json['maxHoursPerWeek'] as num?)?.toInt() ?? 20,
      ratingScore: (json['ratingScore'] as num?)?.toDouble() ?? 5.0,
      skills: rawSkills
          .map((s) => ProfileSkill.fromJson(s as Map<String, dynamic>))
          .toList(),
    );
  }
}

/// A single skill attached to a volunteer's profile.
class ProfileSkill {
  final String id;
  final String name;
  final String category;
  final String proficiencyLevel;

  const ProfileSkill({
    required this.id,
    required this.name,
    required this.category,
    required this.proficiencyLevel,
  });

  factory ProfileSkill.fromJson(Map<String, dynamic> json) {
    return ProfileSkill(
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      category: json['category'] as String? ?? '',
      proficiencyLevel: json['proficiencyLevel'] as String? ?? 'Intermediate',
    );
  }
}