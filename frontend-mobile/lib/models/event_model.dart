class RoleRequirementModel {
  final String id;
  final String roleName;
  final String? description;
  final int requiredHeadcount;
  final String minExperienceLevel;

  RoleRequirementModel({
    required this.id,
    required this.roleName,
    this.description,
    required this.requiredHeadcount,
    required this.minExperienceLevel,
  });

  factory RoleRequirementModel.fromJson(Map<String, dynamic> json) {
    return RoleRequirementModel(
      id: json['id']?.toString() ?? '',
      roleName: json['roleName']?.toString() ?? '',
      description: json['description']?.toString(),
      requiredHeadcount: (json['requiredHeadcount'] as num?)?.toInt() ?? 1,
      minExperienceLevel: json['minExperienceLevel']?.toString() ?? 'Beginner',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'roleName': roleName,
      'description': description,
      'requiredHeadcount': requiredHeadcount,
      'minExperienceLevel': minExperienceLevel,
    };
  }
}

class EventModel {
  final String id;
  final String title;
  final String description;
  final String category;
  final DateTime startDate;
  final DateTime endDate;
  final String status;
  final String? venueName;
  final List<RoleRequirementModel> roleRequirements;

  EventModel({
    required this.id,
    required this.title,
    required this.description,
    required this.category,
    required this.startDate,
    required this.endDate,
    required this.status,
    this.venueName,
    required this.roleRequirements,
  });

  factory EventModel.fromJson(Map<String, dynamic> json) {
    var rawRoles = json['roleRequirements'] as List<dynamic>? ?? [];
    List<RoleRequirementModel> roles = rawRoles
        .map((r) => RoleRequirementModel.fromJson(r as Map<String, dynamic>))
        .toList();

    DateTime start = DateTime.tryParse(json['startDate']?.toString() ?? '') ?? DateTime.now();
    DateTime end = DateTime.tryParse(json['endDate']?.toString() ?? '') ?? DateTime.now();

    return EventModel(
      id: json['id']?.toString() ?? '',
      title: json['title']?.toString() ?? 'Untitled Event',
      description: json['description']?.toString() ?? '',
      category: json['category']?.toString() ?? 'General',
      startDate: start,
      endDate: end,
      status: json['status']?.toString() ?? 'Published',
      venueName: json['venueName']?.toString() ?? json['venue']?['name']?.toString() ?? 'Main Arena',
      roleRequirements: roles,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'description': description,
      'category': category,
      'startDate': startDate.toIso8601String(),
      'endDate': endDate.toIso8601String(),
      'status': status,
      'venueName': venueName,
      'roleRequirements': roleRequirements.map((r) => r.toJson()).toList(),
    };
  }
}
