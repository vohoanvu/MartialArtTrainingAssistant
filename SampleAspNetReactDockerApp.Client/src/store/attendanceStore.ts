import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { AttendanceRecordDto, SessionDetailViewModel } from '@/types/global';

interface AttendanceState {
    // Map of sessionId to attendance records
    sessionRecords: Record<string, AttendanceRecordDto[]>;
    // Map of sessionId to session details
    sessionDetails: Record<string, SessionDetailViewModel>;
    // Actions
    setSessionRecords: (sessionId: string, records: AttendanceRecordDto[]) => void;
    updateRecord: (sessionId: string, index: number, field: keyof AttendanceRecordDto, value: any) => void;
    addEmptyRecords: (sessionId: string) => void;
    clearSessionRecords: (sessionId: string) => void;
    setSessionDetailViewModel: (sessionId: string, details: SessionDetailViewModel) => void;
}

export const useAttendanceStore = create<AttendanceState>()(
    persist(
        (set) => ({
            sessionRecords: {},
            sessionDetails: {},

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

            setSessionDetailViewModel: (sessionId, details) =>
                set((state) => ({
                    sessionDetails: {
                        ...state.sessionDetails,
                        [sessionId]: details
                    }
                }))
        }),
        {
            name: 'attendance-storage', // unique name for localStorage
            partialize: (state) => ({
                sessionRecords: state.sessionRecords,
                sessionDetails: state.sessionDetails
            }) // persist both sessionRecords and sessionDetails
        }
    )
);