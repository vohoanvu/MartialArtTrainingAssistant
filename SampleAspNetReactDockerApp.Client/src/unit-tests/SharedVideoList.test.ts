import { render, screen, waitFor } from '@testing-library/react';
import { Home } from '../pages/Home';
import * as authStore from '@/store/authStore';
import * as apiService from '@/services/api';
import { UseBoundStore } from 'zustand';
import AuthStore from '@/store/authStore';
import '@testing-library/jest-dom';

// Mock the useAuthStore hook
jest.mock('@/store/authStore', () => ({
    __esModule: true,
    default: jest.fn(),
}));

// Mock the getAllSharedVideos API call
jest.mock('@/services/api', () => ({
  getAllSharedVideos: jest.fn(),
}));

// Create a type for the mocked useAuthStore
type MockUseAuthStore = UseBoundStore<typeof AuthStore> & {
  mockReturnValue: jest.Mock;
};

// Cast the mock to the new type with Jest's mock functions
const mockedUseAuthStore = authStore.default as jest.MockedFunction<typeof authStore.default>;

describe('SharedVideosList', () => {
    it('displays videos when fetched from the API', async () => {
        // Setup mock return values
        const mockVideos = [
        {
            id: 'video1',
            title: 'Test Video 1',
            embedLink: 'https://www.example.com/embed/video1',
            sharedBy: { username: 'user1' },
            description: 'Description for Test Video 1',
        },
        // Add more mock video objects as needed
        ];
        const mockAuthStore = {
            accessToken: 'mockAccessToken',
            refreshToken: 'mockRefreshToken',
        };

        (mockedUseAuthStore as unknown as MockUseAuthStore).mockReturnValue(mockAuthStore);
        (apiService.getAllSharedVideos as jest.Mock).mockResolvedValue(mockVideos);

        // Render the component
        render(typeof Home);

        // Wait for the videos to be fetched and displayed
        await waitFor(() => {
            mockVideos.forEach((video) => {
                expect(screen.getByText(video.title)).toBeInTheDocument();
                expect(screen.getByText(`Shared by: ${video.sharedBy.username}`)).toBeInTheDocument();
                expect(screen.getByText(video.description)).toBeInTheDocument();
            });
        });
    });

    it('displays loading message when fetching videos', () => {
        // Render the component
        render(typeof Home);

        // Check for the loading message
        expect(screen.getByText('Loading data from API...')).toBeInTheDocument();
    });
});