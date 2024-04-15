import { render, screen, waitFor } from '@testing-library/react';
import { Home } from '../pages/Home';
import '@testing-library/jest-dom';
import React from 'react';
import FetchMock, {enableFetchMocks} from 'jest-fetch-mock';
import { act } from 'react-dom/test-utils';
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(globalThis as any).fetch = FetchMock;
enableFetchMocks();


describe('SharedVideosList', () => {
    it('displays videos when fetched from the API', async () => {
        // Setup mock return values
        const mockVideos = [
            {
                id: 'videoId1',
                title: 'Test Video 1',
                embedLink: 'https://www.example.com/embed/video1',
                sharedBy: { userId: 'userid1', username: 'user1' },
                description: 'Description for Test Video 1',
            },
            {
                id: 'videoId2',
                title: 'Test Video 2',
                embedLink: 'https://www.example.com/embed/video2',
                sharedBy: { userId: 'userid2', username: 'user2' },
                description: 'Description for Test Video 2',
            }
        ];
        // Mock the getAllSharedVideos API call
        jest.mock('@/services/api', () => ({
            getAllSharedVideos: jest.fn().mockResolvedValue(mockVideos),
            useAuthStore: jest.fn().mockReturnValue(mockAuthStore),
        }));
        const mockAuthStore = {
            accessToken: 'mockAccessToken',
            refreshToken: 'mockRefreshToken',
        };
        FetchMock.mockResponseOnce(JSON.stringify(mockVideos));

        // Wrap the render call in act
        await act(async () => {
            render(React.createElement(Home));
        });

        // Wait for the videos to be fetched and displayed
        await waitFor(() => {
            mockVideos.forEach((video) => {
                expect(screen.getByText(video.title)).toBeInTheDocument();
                expect(screen.getByText(`Shared by: ${video.sharedBy.username}`)).toBeInTheDocument();
                expect(screen.getByText(new RegExp(`Description for ${video.title}`, 'i'))).toBeInTheDocument();
            });
        });
    });

    it('displays loading message when fetching videos', async () => {
        // Mock the getAllSharedVideos API call to return a promise that doesn't resolve immediately
        jest.mock('@/services/api', () => ({
            getAllSharedVideos: jest.fn().mockReturnValue(new Promise(() => {})), // This promise never resolves
            useAuthStore: jest.fn().mockReturnValue(mockAuthStore),
        }));
        const mockAuthStore = {
            accessToken: 'mockAccessToken',
            refreshToken: 'mockRefreshToken',
        };
        FetchMock.mockResponseOnce(JSON.stringify(new Promise(() => {})));

        await act(async () => {
            render(React.createElement(Home));
        });

        expect(screen.getByText(/No Video yet shared/i)).toBeInTheDocument();
    });
});
