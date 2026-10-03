import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/api/api_client.dart';
import '../../data/models/shift_model.dart';
import '../../data/repositories/shift_repository.dart';
import '../../../shift_swaps/presentation/screens/create_swap_screen.dart';

/// Screen displaying comprehensive details of a selected shift.
class ShiftDetailScreen extends StatefulWidget {
  const ShiftDetailScreen({
    super.key,
    required this.shiftId,
    this.shift,
    this.canRequestSwap = false,
    this.isAssigned = false,
    this.assignmentStatus,
    this.apiClient,
  });

  final String shiftId;
  final ShiftModel? shift;
  final bool canRequestSwap;
  final bool isAssigned;
  final String? assignmentStatus;
  final ApiClient? apiClient;

  @override
  State<ShiftDetailScreen> createState() => _ShiftDetailScreenState();
}

class _ShiftDetailScreenState extends State<ShiftDetailScreen> {
  late final ShiftRepository _shiftRepository = ShiftRepository(
    apiClient: widget.apiClient,
  );

  bool _isLoading = false;
  String? _errorMessage;
  ShiftModel? _shift;

  @override
  void initState() {
    super.initState();
    _shift = widget.shift;
    _fetchShift();
  }

  Future<void> _fetchShift() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final shift = await _shiftRepository.getShiftById(widget.shiftId);
      if (!mounted) return;
      setState(() {
        _shift = shift;
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
        title: Text(_shift?.title ?? 'Shift Details'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: _fetchShift),
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
              Text(_errorMessage!, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              ElevatedButton.icon(
                onPressed: _fetchShift,
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    final shift = _shift;
    if (shift == null) {
      return const Center(child: Text('Shift not found'));
    }

    final dateFormat = DateFormat('EEEE, MMMM d, yyyy');
    final timeFormat = DateFormat('h:mm a');

    final dateStr = dateFormat.format(shift.startTime);
    final startStr = timeFormat.format(shift.startTime);
    final endStr = timeFormat.format(shift.endTime);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Card(
            elevation: 2,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Container(
                        padding: const EdgeInsets.all(10),
                        decoration: BoxDecoration(
                          color: Colors.deepPurple.shade50,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: const Icon(
                          Icons.schedule,
                          color: Colors.deepPurple,
                          size: 28,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              shift.title,
                              style: const TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                            Text(
                              shift.eventName,
                              style: const TextStyle(
                                fontSize: 16,
                                color: Colors.deepPurple,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                  const Divider(height: 24),
                  _buildDetailRow(Icons.calendar_today, 'Date', dateStr),
                  const SizedBox(height: 12),
                  _buildDetailRow(
                    Icons.access_time,
                    'Time',
                    '$startStr - $endStr',
                  ),
                  const SizedBox(height: 12),
                  _buildDetailRow(
                    Icons.work_outline,
                    'Role Requirement',
                    shift.roleRequirementName,
                  ),
                  const SizedBox(height: 12),
                  _buildDetailRow(
                    Icons.people_outline,
                    'Capacity',
                    '${shift.assignedCount} assigned / ${shift.capacity} max (${shift.remainingCapacity} spots remaining)',
                  ),
                  const SizedBox(height: 12),
                  _buildDetailRow(
                    Icons.flag_outlined,
                    'Shift Status',
                    shift.status,
                  ),
                  if (widget.isAssigned) ...[
                    const SizedBox(height: 12),
                    _buildDetailRow(
                      Icons.assignment_turned_in_outlined,
                      'My Assignment',
                      widget.assignmentStatus ?? 'Assigned',
                    ),
                  ],
                ],
              ),
            ),
          ),
          if (widget.canRequestSwap) ...[
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: () {
                  Navigator.of(context).push(
                    MaterialPageRoute<bool>(
                      builder: (_) => CreateSwapScreen(
                        sourceShift: shift,
                        apiClient: widget.apiClient,
                      ),
                    ),
                  );
                },
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  backgroundColor: Colors.deepPurple,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                icon: const Icon(Icons.swap_horiz),
                label: const Text(
                  'Request Shift Swap',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildDetailRow(IconData icon, String title, String value) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon, size: 20, color: Colors.black54),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(
                  fontSize: 12,
                  color: Colors.black54,
                  fontWeight: FontWeight.w500,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: const TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: Colors.black87,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
