import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/api/api_client.dart';
import '../../data/models/shift_assignment_model.dart';
import '../../data/repositories/shift_repository.dart';
import 'shift_detail_screen.dart';

/// Screen listing shifts assigned to the currently authenticated volunteer.
class MyShiftsScreen extends StatefulWidget {
  const MyShiftsScreen({super.key, this.apiClient});

  final ApiClient? apiClient;

  @override
  State<MyShiftsScreen> createState() => _MyShiftsScreenState();
}

class _MyShiftsScreenState extends State<MyShiftsScreen> {
  late final ShiftRepository _shiftRepository = ShiftRepository(
    apiClient: widget.apiClient,
  );

  bool _isLoading = true;
  String? _errorMessage;
  List<ShiftAssignmentModel> _assignments = [];

  @override
  void initState() {
    super.initState();
    _loadMyShifts();
  }

  Future<void> _loadMyShifts() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final result = await _shiftRepository.getMyAssignments();
      if (!mounted) return;
      setState(() {
        _assignments = result.assignments;
        _isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Assigned Shifts'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _loadMyShifts,
          ),
        ],
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.error_outline,
                size: 64,
                color: Colors.redAccent,
              ),
              const SizedBox(height: 16),
              const Text(
                'Could not load your shifts',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                _errorMessage!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.black54),
              ),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _loadMyShifts,
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    if (_assignments.isEmpty) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.event_available, size: 64, color: Colors.grey),
              const SizedBox(height: 16),
              const Text(
                'No shifts assigned yet',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              const Text(
                'Shifts assigned to you by organizers will appear here.',
                textAlign: TextAlign.center,
                style: TextStyle(color: Colors.black54),
              ),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _loadMyShifts,
                icon: const Icon(Icons.refresh),
                label: const Text('Refresh'),
              ),
            ],
          ),
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadMyShifts,
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        itemCount: _assignments.length,
        itemBuilder: (context, index) {
          final assignment = _assignments[index];
          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: ListTile(
              contentPadding: const EdgeInsets.all(16),
              title: Text(
                assignment.title,
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
              subtitle: Padding(
                padding: const EdgeInsets.only(top: 8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(assignment.eventName),
                    const SizedBox(height: 4),
                    Text(
                      '${DateFormat('EEE, MMM d, yyyy').format(assignment.startTime)} · '
                      '${DateFormat('h:mm a').format(assignment.startTime)}–'
                      '${DateFormat('h:mm a').format(assignment.endTime)}',
                    ),
                    const SizedBox(height: 4),
                    Text('Role: ${assignment.roleRequirementName}'),
                    const SizedBox(height: 8),
                    Chip(
                      visualDensity: VisualDensity.compact,
                      label: Text(assignment.status),
                    ),
                  ],
                ),
              ),
              trailing: const Icon(Icons.chevron_right),
              onTap: () {
                Navigator.of(context).push(
                  MaterialPageRoute<void>(
                    builder: (_) => ShiftDetailScreen(
                      shiftId: assignment.shiftId,
                      canRequestSwap: assignment.status == 'Confirmed',
                      isAssigned: true,
                      assignmentStatus: assignment.status,
                      apiClient: widget.apiClient,
                    ),
                  ),
                );
              },
            ),
          );
        },
      ),
    );
  }
}
