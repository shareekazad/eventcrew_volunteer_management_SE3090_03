import 'skill_model.dart';

class VolunteerProfileModel {
  final String id;
  final String userId;
  final String fullName;
  final String email;
  final String emergencyContact;
  final String bio;
  final int maxHoursPerWeek;
  final double ratingScore;
  final List<SkillModel> skills;

  VolunteerProfileModel({
    required this.id,
    required this.userId,
    required this.fullName,
    required this.email,
    required this.emergencyContact,
    required this.bio,
    required this.maxHoursPerWeek,
    required this.ratingScore,
    required this.skills,
  });

  factory VolunteerProfileModel.fromJson(Map<String, dynamic> json) {
    var rawSkills = json['skills'] as List<dynamic>? ?? [];
    List<SkillModel> parsedSkills =
        rawSkills.map((s) => SkillModel.fromJson(s as Map<String, dynamic>)).toList();

    return VolunteerProfileModel(
      id: json['id']?.toString() ?? '',
      userId: json['userId']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? '',
      email: json['email']?.toString() ?? '',
      emergencyContact: json['emergencyContact']?.toString() ?? '',
      bio: json['bio']?.toString() ?? '',
      maxHoursPerWeek: (json['maxHoursPerWeek'] as num?)?.toInt() ?? 20,
      ratingScore: (json['ratingScore'] as num?)?.toDouble() ?? 5.0,
      skills: parsedSkills,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'userId': userId,
      'fullName': fullName,
      'email': email,
      'emergencyContact': emergencyContact,
      'bio': bio,
      'maxHoursPerWeek': maxHoursPerWeek,
      'ratingScore': ratingScore,
      'skills': skills.map((s) => s.toJson()).toList(),
    };
  }
}
