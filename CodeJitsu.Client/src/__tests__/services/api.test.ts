import { describe, it, expect, vi } from 'vitest';
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';
import {
  getMyTrainingSessions,
  createTrainingSession,
  getAllSharedVideos,
  deleteUploadedVideo,
  getUploadedVideos,
  getTrainingSessionDetails,
  closeTrainingSession,
  deleteTrainingSession,
  getFighterDetails,
  generateClassCurriculum,
  getClassCurriculum,
  joinWailList,
  takeWalkInAttendance,
  youtubeSearch,
  getVideoFeedback,
} from '@/services/api';

const noopHydrate = vi.fn().mockResolvedValue(undefined);

describe('getMyTrainingSessions', () => {
  it('should return an array of training sessions', async () => {
    const sessions = await getMyTrainingSessions({
      jwtToken: 'test-token',
      refreshToken: 'refresh-token',
      hydrate: noopHydrate,
    });
    expect(Array.isArray(sessions)).toBe(true);
    expect(sessions.length).toBeGreaterThan(0);
  });

  it('should throw an error when the API returns a non-2xx non-401 response', async () => {
    server.use(
      http.get('/api/trainingsession', () => new HttpResponse(null, { status: 500 }))
    );
    await expect(
      getMyTrainingSessions({ jwtToken: null, refreshToken: null, hydrate: noopHydrate })
    ).rejects.toThrow();
  });
});

describe('createTrainingSession', () => {
  it('should return the created session on success', async () => {
    const newSession = {
      trainingDate: '2026-03-27T09:00:00',
      description: 'Test session',
      capacity: 20,
      duration: 1.5,
      status: 'Active' as const,
      targetLevel: 'Beginner' as const,
    };
    const result = await createTrainingSession(newSession, 'test-token');
    expect(result).toHaveProperty('id');
  });

  it('should throw when the API returns an error', async () => {
    server.use(
      http.post('/api/trainingsession', () => new HttpResponse(null, { status: 400 }))
    );
    await expect(
      createTrainingSession(
        { trainingDate: '', description: '', capacity: 0, duration: 0, status: 'Active', targetLevel: 'Beginner' },
        'token'
      )
    ).rejects.toThrow();
  });
});

describe('getAllSharedVideos', () => {
  it('should return shared videos array', async () => {
    const videos = await getAllSharedVideos({
      jwtToken: 'test-token',
      refreshToken: 'refresh-token',
      hydrate: noopHydrate,
    });
    expect(Array.isArray(videos)).toBe(true);
    expect(videos.length).toBeGreaterThan(0);
    expect(videos[0]).toHaveProperty('id');
  });

  it('should throw when API returns server error', async () => {
    server.use(
      http.get('/vid/api/video/getall', () => new HttpResponse(null, { status: 500 }))
    );
    await expect(
      getAllSharedVideos({ jwtToken: 'token', refreshToken: 'refresh', hydrate: noopHydrate })
    ).rejects.toThrow();
  });
});

describe('getUploadedVideos', () => {
  it('should return uploaded videos array', async () => {
    const videos = await getUploadedVideos({
      jwtToken: 'test-token',
      refreshToken: 'refresh-token',
      hydrate: noopHydrate,
    });
    expect(Array.isArray(videos)).toBe(true);
    expect(videos.length).toBeGreaterThan(0);
    expect(videos[0]).toHaveProperty('fileName');
  });
});

describe('deleteUploadedVideo', () => {
  it('should return success response on delete', async () => {
    const result = await deleteUploadedVideo({
      videoId: 1,
      jwtToken: 'test-token',
      hydrate: noopHydrate,
    });
    expect(result).toHaveProperty('success', true);
  });

  it('should throw when API returns error', async () => {
    server.use(
      http.delete('/vid/api/video/delete-uploaded/:videoId', () =>
        new HttpResponse('Not found', { status: 404 })
      )
    );
    await expect(
      deleteUploadedVideo({ videoId: 999, jwtToken: 'token', hydrate: noopHydrate })
    ).rejects.toThrow();
  });
});

describe('getTrainingSessionDetails', () => {
  it('should return session details for a valid session id', async () => {
    const details = await getTrainingSessionDetails(1, {
      jwtToken: 'token',
      refreshToken: 'refresh',
      hydrate: noopHydrate,
    });
    expect(details).toHaveProperty('id', 1);
  });

  it('should throw when session is not found (404)', async () => {
    await expect(
      getTrainingSessionDetails(999, {
        jwtToken: 'token',
        refreshToken: 'refresh',
        hydrate: noopHydrate,
      })
    ).rejects.toThrow();
  });
});

describe('closeTrainingSession', () => {
  it('should resolve without error on success', async () => {
    await expect(
      closeTrainingSession(1, { jwtToken: 'token', refreshToken: 'refresh', hydrate: noopHydrate })
    ).resolves.toBeUndefined();
  });

  it('should throw on non-OK response', async () => {
    server.use(
      http.patch('/api/trainingsession/:sessionId/close', () =>
        new HttpResponse(null, { status: 500 })
      )
    );
    await expect(
      closeTrainingSession(1, { jwtToken: 'token', refreshToken: 'refresh', hydrate: noopHydrate })
    ).rejects.toThrow();
  });
});

describe('deleteTrainingSession', () => {
  it('should resolve without error on success', async () => {
    await expect(
      deleteTrainingSession(1, { jwtToken: 'token', refreshToken: 'refresh', hydrate: noopHydrate })
    ).resolves.toBeUndefined();
  });
});

describe('getFighterDetails', () => {
  it('should return fighter details for a valid fighter id', async () => {
    const fighter = await getFighterDetails({
      fighterId: 1,
      jwtToken: 'token',
      refreshToken: 'refresh',
      hydrate: noopHydrate,
    });
    expect(fighter).toHaveProperty('id', 1);
  });

  it('should throw when fighter is not found (404)', async () => {
    await expect(
      getFighterDetails({
        fighterId: 999,
        jwtToken: 'token',
        refreshToken: 'refresh',
        hydrate: noopHydrate,
      })
    ).rejects.toThrow();
  });
});

describe('generateClassCurriculum', () => {
  it('should return curriculum data', async () => {
    const curriculum = await generateClassCurriculum({
      sessionId: 1,
      jwtToken: 'token',
      refreshToken: 'refresh',
      hydrate: noopHydrate,
    });
    expect(curriculum).toHaveProperty('sessionId', 1);
    expect(curriculum).toHaveProperty('content');
  });
});

describe('getClassCurriculum', () => {
  it('should return curriculum data for a session', async () => {
    const curriculum = await getClassCurriculum({
      sessionId: 1,
      jwtToken: 'token',
      refreshToken: 'refresh',
      hydrate: noopHydrate,
    });
    expect(curriculum).toHaveProperty('sessionId', 1);
  });
});

describe('joinWailList', () => {
  it('should return 200 status on success', async () => {
    const result = await joinWailList({ email: 'new@example.com', role: 'Instructor', region: 'North America' });
    expect(result).toBe(200);
  });
});

describe('takeWalkInAttendance', () => {
  it('should return a response with success or attendee info', async () => {
    const result = await takeWalkInAttendance(1, {
      records: [
        {
          fighterName: 'Walk In Fighter',
          birthdate: new Date('1995-01-01'),
          weight: 70,
          height: 175,
          beltColor: 'White',
          gender: 'Male',
        },
      ],
    });
    // MSW returns { success: true, attendeeId: 42 }
    expect(result).toBeDefined();
  });
});

describe('youtubeSearch', () => {
  it('should return a text response', async () => {
    const result = await youtubeSearch({
      techniqueName: 'guard pass',
      trainingSessionId: 1,
      jwtToken: 'token',
      refreshToken: 'refresh',
      hydrate: noopHydrate,
    });
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });
});

describe('getVideoFeedback', () => {
  it('should return video feedback with feedback field', async () => {
    const result = await getVideoFeedback({
      videoId: '1',
      jwtToken: 'token',
      refreshToken: 'refresh',
      hydrate: noopHydrate,
    });
    expect(result).toHaveProperty('feedback');
  });
});
