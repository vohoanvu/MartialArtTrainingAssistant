import { describe, it, expect, beforeEach } from 'vitest';
import { act } from '@testing-library/react';
import { useAttendanceStore } from '@/store/attendanceStore';
import { AttendanceRecordDto } from '@/types/global';

const sampleRecord: AttendanceRecordDto = {
  fighterName: 'John Doe',
  birthdate: new Date('1995-06-15'),
  weight: 75,
  height: 175,
  beltColor: 'Blue',
  gender: 'Male',
};

const anotherRecord: AttendanceRecordDto = {
  fighterName: 'Jane Smith',
  birthdate: new Date('1998-03-10'),
  weight: 60,
  height: 165,
  beltColor: 'White',
  gender: 'Female',
};

beforeEach(() => {
  useAttendanceStore.setState({ sessionRecords: {} });
  localStorage.clear();
});

describe('attendanceStore - setSessionRecords', () => {
  it('should set records for a given session id', () => {
    act(() => {
      useAttendanceStore.getState().setSessionRecords(1, [sampleRecord]);
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[1]).toHaveLength(1);
    expect(sessionRecords[1][0].fighterName).toBe('John Doe');
  });

  it('should overwrite existing records for the same session id', () => {
    act(() => {
      useAttendanceStore.getState().setSessionRecords(1, [sampleRecord]);
      useAttendanceStore.getState().setSessionRecords(1, [anotherRecord]);
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[1]).toHaveLength(1);
    expect(sessionRecords[1][0].fighterName).toBe('Jane Smith');
  });
});

describe('attendanceStore - updateRecord', () => {
  it('should update a specific field of a record at a given index', () => {
    act(() => {
      useAttendanceStore.getState().setSessionRecords(1, [sampleRecord, anotherRecord]);
      useAttendanceStore.getState().updateRecord(1, 0, 'fighterName', 'Updated Name');
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[1][0].fighterName).toBe('Updated Name');
    // Other record should remain unchanged
    expect(sessionRecords[1][1].fighterName).toBe('Jane Smith');
  });

  it('should update a numeric field correctly', () => {
    act(() => {
      useAttendanceStore.getState().setSessionRecords(2, [sampleRecord]);
      useAttendanceStore.getState().updateRecord(2, 0, 'weight', 80);
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[2][0].weight).toBe(80);
  });
});

describe('attendanceStore - addEmptyRecords', () => {
  it('should add two empty records to an empty session', () => {
    act(() => {
      useAttendanceStore.getState().addEmptyRecords(3);
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[3]).toHaveLength(2);
    expect(sessionRecords[3][0].fighterName).toBe('');
    expect(sessionRecords[3][0].beltColor).toBe('White');
    expect(sessionRecords[3][0].gender).toBe('Male');
  });

  it('should append two empty records to existing records', () => {
    act(() => {
      useAttendanceStore.getState().setSessionRecords(3, [sampleRecord]);
      useAttendanceStore.getState().addEmptyRecords(3);
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[3]).toHaveLength(3);
    expect(sessionRecords[3][0].fighterName).toBe('John Doe');
    expect(sessionRecords[3][1].fighterName).toBe('');
  });
});

describe('attendanceStore - clearSessionRecords', () => {
  it('should remove records for a specific session id', () => {
    act(() => {
      useAttendanceStore.getState().setSessionRecords(1, [sampleRecord]);
      useAttendanceStore.getState().setSessionRecords(2, [anotherRecord]);
      useAttendanceStore.getState().clearSessionRecords(1);
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[1]).toBeUndefined();
    // Other session should remain
    expect(sessionRecords[2]).toHaveLength(1);
  });
});

describe('attendanceStore - multiple sessions', () => {
  it('should maintain records independently across multiple session ids', () => {
    act(() => {
      useAttendanceStore.getState().setSessionRecords(10, [sampleRecord]);
      useAttendanceStore.getState().setSessionRecords(20, [anotherRecord, sampleRecord]);
    });
    const { sessionRecords } = useAttendanceStore.getState();
    expect(sessionRecords[10]).toHaveLength(1);
    expect(sessionRecords[20]).toHaveLength(2);
    expect(sessionRecords[10][0].fighterName).toBe('John Doe');
    expect(sessionRecords[20][0].fighterName).toBe('Jane Smith');
  });
});
