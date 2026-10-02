import 'role_requirement_model.dart';

/// Represents an event as returned by GET /api/Events.
///
/// Mirrors the ASP.NET Core EventResponseDto shape.
class EventModel {
  final String id;
  final String organizerId;
  final String? venueId;
  final String title;
  final String? description;
  final String category;
  final DateTime startDate;
  final DateTime endDate;
  final String status;
  final List<RoleRequirementModel> roleRequirements;

  const EventModel({
    required this.id,
    required this.organizerId,
    required this.venueId,
    required this.title,
    required this.description,
    required this.category,
    required this.startDate,
    required this.endDate,
    required this.status,
    required this.roleRequirements,
  });

  /// Constructs an EventModel from the JSON returned by the API.
  factory EventModel.fromJson(Map<String, dynamic> json) {
    final rawRoles = json['roleRequirements'] as List<dynamic>? ?? [];

    return EventModel(
      id: json['id'] as String,
      organizerId: json['organizerId'] as String,
      venueId: json['venueId'] as String?,
      title: json['title'] as String,
      description: json['description'] as String?,
      category: json['category'] as String,
      startDate: DateTime.parse(json['startDate'] as String),
      endDate: DateTime.parse(json['endDate'] as String),
      status: json['status'] as String,
      roleRequirements: rawRoles
          .map((r) => RoleRequirementModel.fromJson(r as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Human-readable short date, e.g. "15 Nov 2026".
  String get formattedDate {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    return '${startDate.day} ${months[startDate.month - 1]} ${startDate.year}';
  }

  /// Total staff required across all roles.
  int get totalRequiredStaff =>
      roleRequirements.fold(0, (sum, r) => sum + r.requiredHeadcount);
}