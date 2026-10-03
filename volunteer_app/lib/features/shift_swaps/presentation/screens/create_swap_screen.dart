import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../../../core/api/api_client.dart';
import '../../../shifts/data/models/shift_assignment_model.dart';
import '../../../shifts/data/models/shift_model.dart';
import '../../../shifts/data/repositories/shift_repository.dart';
import '../../data/repositories/shift_swap_repository.dart';

/// Screen allowing a volunteer to request a shift swap.
class CreateSwapScreen extends StatefulWidget {
  const CreateSwapScreen({super.key, this.sourceShift, this.apiClient});

  final ShiftModel? sourceShift;
  final ApiClient? apiClient;

  @override
  State<CreateSwapScreen> createState() => _CreateSwapScreenState();
}

class _CreateSwapScreenState extends State<CreateSwapScreen> {
  final _formKey = GlobalKey<FormState>();
  late final ShiftRepository _shiftRepository = ShiftRepository(
    apiClient: widget.apiClient,
  );
  late final ShiftSwapRepository _swapRepository = ShiftSwapRepository(
    apiClient: widget.apiClient,
  );

  final _targetVolunteerIdController = TextEditingController();
  final _reasonController = TextEditingController();

  bool _isLoadingShifts = true;
  bool _isSubmitting = false;
  String? _errorMessage;
  List<ShiftAssignmentModel> _myAssignments = [];
  ShiftAssignmentModel? _selectedSourceAssignment;
  List<ShiftModel> _availableShifts = [];
  ShiftModel? _selectedTargetShift;

  @override
  void initState() {
    super.initState();
    _loadAvailableShifts();
  }

  @override
  void dispose() {
    _targetVolunteerIdController.dispose();
    _reasonController.dispose();
    super.dispose();
  }

  Future<void> _loadAvailableShifts() async {
    setState(() {
      _isLoadingShifts = true;
      _errorMessage = null;
    });

    try {
      final volunteerAssignments = await _shiftRepository.getMyAssignments();
      final shifts = await _shiftRepository.getAllShifts();
      if (!mounted) return;
      setState(() {
        _myAssignments = volunteerAssignments.assignments
            .where((assignment) => assignment.status == 'Confirmed')
            .toList();
        _availableShifts = shifts;
        for (final assignment in volunteerAssignments.assignments) {
          if (assignment.shiftId == widget.sourceShift?.id) {
            _selectedSourceAssignment = assignment;
            break;
          }
        }
        _isLoadingShifts = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _errorMessage = e.toString();
        _isLoadingShifts = false;
      });
    }
  }

  Future<void> _handleSubmit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedSourceAssignment == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please select one of your confirmed shifts'),
        ),
      );
      return;
    }
    if (_selectedTargetShift == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select a target shift')),
      );
      return;
    }

    setState(() {
      _isSubmitting = true;
      _errorMessage = null;
    });

    try {
      await _swapRepository.createShiftSwap(
        requesterAssignmentId: _selectedSourceAssignment!.assignmentId,
        targetVolunteerId: _targetVolunteerIdController.text.trim(),
        targetShiftId: _selectedTargetShift!.id,
        reason: _reasonController.text.trim().isNotEmpty
            ? _reasonController.text.trim()
            : null,
      );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Shift swap request submitted successfully!'),
            backgroundColor: Colors.green,
          ),
        );
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = e.toString();
          _isSubmitting = false;
        });
      }
    }
  }

  List<ShiftModel> get _targetShifts => _availableShifts
      .where((shift) => shift.id != _selectedSourceAssignment?.shiftId)
      .toList();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Request Shift Swap')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Card(
                color: Colors.deepPurple.shade50,
                elevation: 0,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'SOURCE SHIFT',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.bold,
                          color: Colors.deepPurple,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        _selectedSourceAssignment?.title ??
                            widget.sourceShift?.title ??
                            'Select a confirmed assignment below',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Text(
                        _selectedSourceAssignment == null
                            ? 'Your confirmed assignment is required to request a swap.'
                            : '${_selectedSourceAssignment!.eventName} · ${_selectedSourceAssignment!.roleRequirementName}\n${DateFormat('EEE, MMM d · h:mm a').format(_selectedSourceAssignment!.startTime)} – ${DateFormat('h:mm a').format(_selectedSourceAssignment!.endTime)}',
                        style: const TextStyle(color: Colors.black87),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 20),
              if (_errorMessage != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.red.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.red.shade200),
                  ),
                  child: Text(
                    _errorMessage!,
                    style: const TextStyle(color: Colors.red),
                  ),
                ),
                const SizedBox(height: 16),
              ],
              _isLoadingShifts
                  ? const Center(child: CircularProgressIndicator())
                  : DropdownButtonFormField<ShiftAssignmentModel>(
                      decoration: const InputDecoration(
                        labelText: 'Your confirmed shift',
                        prefixIcon: Icon(Icons.event_available),
                        border: OutlineInputBorder(),
                      ),
                      initialValue: _selectedSourceAssignment,
                      items: _myAssignments.map((assignment) {
                        return DropdownMenuItem<ShiftAssignmentModel>(
                          value: assignment,
                          child: Text(
                            '${assignment.title} (${assignment.eventName})',
                            overflow: TextOverflow.ellipsis,
                          ),
                        );
                      }).toList(),
                      validator: (assignment) => assignment == null
                          ? 'Select one of your confirmed assignments'
                          : null,
                      onChanged: (assignment) {
                        setState(() {
                          _selectedSourceAssignment = assignment;
                          _selectedTargetShift = null;
                        });
                      },
                    ),
              if (!_isLoadingShifts && _myAssignments.isEmpty) ...[
                const SizedBox(height: 8),
                const Text(
                  'You need a confirmed shift assignment before requesting a swap.',
                  style: TextStyle(color: Colors.black54),
                ),
              ],
              const SizedBox(height: 16),
              TextFormField(
                controller: _targetVolunteerIdController,
                decoration: const InputDecoration(
                  labelText: 'Target Volunteer Profile ID',
                  hintText: 'Enter volunteer UUID',
                  helperText: 'Ask the volunteer for their profile ID. The API does not expose a volunteer directory.',
                  prefixIcon: Icon(Icons.person),
                  border: OutlineInputBorder(),
                ),
                validator: (value) {
                  final id = value?.trim() ?? '';
                  if (id.isEmpty) {
                    return 'Enter the target volunteer profile ID';
                  }
                  if (!RegExp(
                    r'^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$',
                  ).hasMatch(id)) {
                    return 'Enter a valid volunteer profile UUID';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              const Text(
                'The API requires a target volunteer profile UUID and a target shift. The shift is context for the organizer review; the API does not provide a volunteer directory or confirm that the target volunteer holds that shift.',
                style: TextStyle(fontSize: 12, color: Colors.black54),
              ),
              const SizedBox(height: 8),
              _isLoadingShifts
                  ? const Center(child: CircularProgressIndicator())
                  : DropdownButtonFormField<ShiftModel>(
                      decoration: const InputDecoration(
                        labelText: 'Target Shift (organizer review)',
                        prefixIcon: Icon(Icons.swap_horiz),
                        border: OutlineInputBorder(),
                      ),
                      initialValue: _selectedTargetShift,
                      items: _targetShifts.map((shift) {
                        return DropdownMenuItem<ShiftModel>(
                          value: shift,
                          child: Text(
                            '${shift.title} · ${shift.eventName} · ${DateFormat('MMM d, h:mm a').format(shift.startTime)}',
                            overflow: TextOverflow.ellipsis,
                          ),
                        );
                      }).toList(),
                      validator: (shift) =>
                          shift == null ? 'Select a target shift' : null,
                      onChanged: (val) {
                        setState(() {
                          _selectedTargetShift = val;
                        });
                      },
                    ),
              if (!_isLoadingShifts && _targetShifts.isEmpty) ...[
                const SizedBox(height: 8),
                const Text(
                  'No other shifts are available as a swap target.',
                  style: TextStyle(color: Colors.black54),
                ),
              ],
              const SizedBox(height: 16),
              TextFormField(
                controller: _reasonController,
                maxLines: 3,
                maxLength: 500,
                decoration: const InputDecoration(
                  labelText: 'Reason for Swap (optional)',
                  hintText: 'Explain why you are requesting this swap...',
                  border: OutlineInputBorder(),
                ),
              ),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed:
                    _isSubmitting ||
                        _isLoadingShifts ||
                        _myAssignments.isEmpty ||
                        _targetShifts.isEmpty
                    ? null
                    : _handleSubmit,
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  backgroundColor: Colors.deepPurple,
                  foregroundColor: Colors.white,
                ),
                icon: const Icon(Icons.send),
                label: _isSubmitting
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.white,
                        ),
                      )
                    : const Text(
                        'Submit Swap Request',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
