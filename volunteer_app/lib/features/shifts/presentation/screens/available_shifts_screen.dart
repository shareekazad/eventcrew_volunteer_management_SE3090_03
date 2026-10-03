import 'package:flutter/material.dart';

import '../../../../core/api/api_client.dart';
import '../../data/models/shift_model.dart';
import '../../data/repositories/shift_repository.dart';
import '../widgets/shift_card.dart';
import 'shift_detail_screen.dart';

/// Lists shifts returned by the authenticated shifts API.
class AvailableShiftsScreen extends StatefulWidget {
  const AvailableShiftsScreen({super.key, this.apiClient});

  final ApiClient? apiClient;

  @override
  State<AvailableShiftsScreen> createState() => _AvailableShiftsScreenState();
}

class _AvailableShiftsScreenState extends State<AvailableShiftsScreen> {
  late final ShiftRepository _shiftRepository = ShiftRepository(
    apiClient: widget.apiClient,
  );
  bool _isLoading = true;
  String? _errorMessage;
  List<ShiftModel> _shifts = [];
  Set<String> _assignedShiftIds = {};

  @override
  void initState() {
    super.initState();
    _loadShifts();
  }

  Future<void> _loadShifts() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final shiftsFuture = _shiftRepository.getAllShifts();
      final myShiftsFuture = _shiftRepository.getMyShifts();
      final shifts = await shiftsFuture;
      final myShifts = await myShiftsFuture;
      if (!mounted) return;
      setState(() {
        _shifts = shifts;
        _assignedShiftIds = myShifts.map((shift) => shift.id).toSet();
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _errorMessage = error.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Available Shifts'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: _loadShifts,
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
      return _StatusMessage(
        icon: Icons.error_outline,
        title: 'Could not load shifts',
        message: _errorMessage!,
        action: _loadShifts,
      );
    }

    if (_shifts.isEmpty) {
      return _StatusMessage(
        icon: Icons.event_busy,
        title: 'No shifts available',
        message: 'Check back later for upcoming volunteer shifts.',
        action: _loadShifts,
      );
    }

    return RefreshIndicator(
      onRefresh: _loadShifts,
      child: ListView.builder(
        padding: const EdgeInsets.all(12),
        itemCount: _shifts.length,
        itemBuilder: (context, index) {
          final shift = _shifts[index];
          return ShiftCard(
            shift: shift,
            isAssigned: _assignedShiftIds.contains(shift.id),
            onTap: () {
              Navigator.of(context).push(
                MaterialPageRoute<void>(
                  builder: (_) => ShiftDetailScreen(
                    shiftId: shift.id,
                    shift: shift,
                    isAssigned: _assignedShiftIds.contains(shift.id),
                    apiClient: widget.apiClient,
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }
}

class _StatusMessage extends StatelessWidget {
  const _StatusMessage({
    required this.icon,
    required this.title,
    required this.message,
    required this.action,
  });

  final IconData icon;
  final String title;
  final String message;
  final VoidCallback action;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 56, color: Colors.grey),
            const SizedBox(height: 16),
            Text(
              title,
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(message, textAlign: TextAlign.center),
            const SizedBox(height: 20),
            OutlinedButton.icon(
              onPressed: action,
              icon: const Icon(Icons.refresh),
              label: const Text('Try again'),
            ),
          ],
        ),
      ),
    );
  }
}
