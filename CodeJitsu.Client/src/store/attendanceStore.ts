import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { AttendanceRecordDto } from '@/types/global';

interface AttendanceState {
    // Map of sessionId to attendance records
    sessionRecords: Record<number, AttendanceRecordDto[]>;
    // Actions
    setSessionRecords: (sessionId: number, records: AttendanceRecordDto[]) => void;
    updateRecord: (sessionId: number, index: number, field: keyof AttendanceRecordDto, value: any) => void;
    addEmptyRecords: (sessionId: number) => void;
    clearSessionRecords: (sessionId: number) => void;
}

export const useAttendanceStore = create<AttendanceState>()(
    persist(
        (set) => ({
            sessionRecords: {},

            setSessionRecords: (sessionId, records) =>
                set((state) => ({
                    sessionRecords: {
                        ...state.sessionRecords,
                        [sessionId]: records
                    }
                })),

            updateRecord: (sessionId, index, field, value) =>
                set((state) => {
                    const sessionRecords = state.sessionRecords[sessionId] || [];
                    const newRecords = [...sessionRecords];
                    newRecords[index] = { ...newRecords[index], [field]: value };
                    return {
                        sessionRecords: {
                            ...state.sessionRecords,
                            [sessionId]: newRecords
                        }
                    };
                }),

            addEmptyRecords: (sessionId) =>
                set((state) => {
                    const sessionRecords = state.sessionRecords[sessionId] || [];
                    const emptyRecord: AttendanceRecordDto = {
                        fighterName: '',
                        birthdate: new Date(),
                        weight: 0,
                        height: 0,
                        beltColor: 'White',
                        gender: 'Male'
                    };
                    return {
                        sessionRecords: {
                            ...state.sessionRecords,
                            [sessionId]: [...sessionRecords, emptyRecord, emptyRecord]
                        }
                    };
                }),

            clearSessionRecords: (sessionId) =>
                set((state) => {
                    const { [sessionId]: _, ...remainingRecords } = state.sessionRecords;
                    return { sessionRecords: remainingRecords };
                }),
        }),
        {
            name: 'attendance-storage', // unique name for localStorage
            partialize: (state) => ({
                sessionRecords: state.sessionRecords,
            }) // persist sessionRecords
        }
    )
);