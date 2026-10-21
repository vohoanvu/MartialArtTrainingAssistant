import React, { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ExternalLink, FileText, YoutubeIcon } from 'lucide-react';

interface YoutubeVideo {
    title: string;
    description: string;
    video_id: string;
    publication_date: string;
    embed_link: string;
    thumbnail_url: string;
    channel_title?: string | null;
    why_relevant?: string | null;
}

interface WebResource {
    title: string;
    url: string;
    description?: string | null;
    is_free?: boolean | null;
}

export interface TechniqueSearchResults {
    source?: 'vertex-grounded' | 'youtube-fallback';
    model_id?: string | null;
    youtube_videos?: YoutubeVideo[];
    web_resources?: WebResource[];
    web_search_queries?: string[];
    search_entry_point_html?: string | null;
}

interface LiveSearchResultsProps {
    jsonContent: string;
}

/** Only http(s) URLs are ever linked/embedded; anything else (javascript:, data:, ...) is dropped. */
const isHttpUrl = (value: unknown): value is string => {
    if (typeof value !== 'string') return false;
    try {
        const { protocol } = new URL(value);
        return protocol === 'http:' || protocol === 'https:';
    } catch {
        return false;
    }
};

const asArray = <T,>(value: unknown): T[] => (Array.isArray(value) ? (value as T[]) : []);

const parseResults = (jsonContent: string): { data: TechniqueSearchResults | null; failed: boolean } => {
    try {
        const parsed = JSON.parse(jsonContent);
        if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
            return { data: parsed as TechniqueSearchResults, failed: false };
        }
        return { data: null, failed: true };
    } catch {
        return { data: null, failed: true };
    }
};

const LiveSearchResults: React.FC<LiveSearchResultsProps> = ({ jsonContent }) => {
    const { t } = useTranslation();
    const { data, failed } = useMemo(() => parseResults(jsonContent), [jsonContent]);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [currentVideo, setCurrentVideo] = useState<string | null>(null);

    if (failed || !data) {
        return <div className="text-blood-300">{t('liveSearch.parseError')}</div>;
    }

    const videos = asArray<YoutubeVideo>(data.youtube_videos);
    const resources = asArray<WebResource>(data.web_resources).filter((r) => r && isHttpUrl(r.url));
    const suggestionsHtml = typeof data.search_entry_point_html === 'string' && data.search_entry_point_html.trim() !== ''
        ? data.search_entry_point_html
        : null;
    const hasContent = videos.length > 0 || resources.length > 0 || suggestionsHtml !== null;

    const openVideoModal = (embedLink: string) => {
        if (!isHttpUrl(embedLink)) return;
        setCurrentVideo(embedLink);
        setIsModalOpen(true);
    };

    return (
        <div className="space-y-8">
            {data.source && (
                <div>
                    <Badge variant={data.source === 'vertex-grounded' ? 'blue' : 'parch'}>
                        {data.source === 'vertex-grounded'
                            ? (data.model_id
                                ? t('liveSearch.sourceGrounded', { model: data.model_id })
                                : t('liveSearch.sourceGroundedNoModel'))
                            : t('liveSearch.sourceYoutube')}
                    </Badge>
                </div>
            )}

            {!hasContent && <p className="text-sm text-slate-zen400">{t('liveSearch.noResults')}</p>}

            {videos.length > 0 && (
                <section>
                    <h2 className="text-xl font-serif font-semibold text-ink-400 mb-4 flex items-center">
                        <YoutubeIcon className="mr-2 text-blood-300" /> {t('liveSearch.videosTitle')}
                    </h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        {videos.map((video, index) => (
                            <VideoCard key={video.video_id ?? index} video={video} onPlay={() => openVideoModal(video.embed_link)} />
                        ))}
                    </div>
                </section>
            )}

            {resources.length > 0 && (
                <section>
                    <h2 className="text-xl font-serif font-semibold text-ink-400 mb-4 flex items-center">
                        <FileText className="mr-2 text-samurai-400" /> {t('liveSearch.articlesTitle')}
                    </h2>
                    <ul className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        {resources.map((resource, index) => (
                            <ResourceItem key={`${resource.url}-${index}`} resource={resource} />
                        ))}
                    </ul>
                </section>
            )}

            {suggestionsHtml && (
                <section>
                    <h3 className="text-xs font-sans font-medium text-slate-zen400 mb-1">{t('liveSearch.searchSuggestions')}</h3>
                    {/* Google's Search Suggestions markup must be shown unmodified. It is isolated in a sandboxed
                        iframe without allow-scripts / allow-same-origin, so it can never run code in this page. */}
                    <iframe
                        title={t('liveSearch.searchSuggestionsFrameTitle')}
                        srcDoc={suggestionsHtml}
                        sandbox="allow-popups allow-popups-to-escape-sandbox"
                        className="w-full h-20 rounded-md border border-[rgba(60,50,40,0.12)] bg-white"
                    />
                </section>
            )}

            <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
                <DialogContent className="max-w-4xl">
                    <DialogHeader>
                        <DialogTitle>{t('liveSearch.videoPlayer')}</DialogTitle>
                    </DialogHeader>
                    {currentVideo && (
                        <iframe
                            title={t('liveSearch.videoPlayer')}
                            width="100%"
                            height="400"
                            src={currentVideo}
                            frameBorder="0"
                            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                            allowFullScreen
                        ></iframe>
                    )}
                </DialogContent>
            </Dialog>
        </div>
    );
};

const formatDate = (value: string | undefined, locale: string): string => {
    if (!value) return '';
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) return value;
    return parsed.toLocaleDateString(locale, { year: 'numeric', month: 'short', day: 'numeric' });
};

const VideoCard: React.FC<{ video: YoutubeVideo; onPlay: () => void }> = ({ video, onPlay }) => {
    const { t, i18n } = useTranslation();
    const thumbnailUrl = isHttpUrl(video.thumbnail_url)
        ? video.thumbnail_url
        : `https://img.youtube.com/vi/${video.video_id}/0.jpg`;
    const date = formatDate(video.publication_date, i18n.language);
    return (
        <div className="bg-parchment-50 p-4 rounded-xl border border-[rgba(60,50,40,0.12)] shadow-zen-sm hover:shadow-zen-md hover:-translate-y-px transition-all duration-fast ease-zen">
            <img src={thumbnailUrl} alt={video.title} className="w-full h-48 object-cover rounded-lg" />
            <h3 className="text-base font-serif font-semibold mt-2 text-ink-400 line-clamp-2">{video.title}</h3>
            <p className="text-xs font-mono text-slate-zen400 mt-1">
                {video.channel_title && <span className="font-sans font-medium">{video.channel_title}</span>}
                {video.channel_title && date && <span aria-hidden="true"> · </span>}
                {date && <time dateTime={video.publication_date}>{date}</time>}
            </p>
            {video.why_relevant && (
                <p className="text-sm mt-2 text-samurai-500">
                    <span className="font-semibold">{t('liveSearch.whyRelevant')}: </span>
                    {video.why_relevant}
                </p>
            )}
            <p className="text-sm mt-2 text-slate-zen500 line-clamp-3">{video.description}</p>
            <Button onClick={onPlay} variant="primary" size="sm" className="mt-3">{t('liveSearch.playVideo')}</Button>
        </div>
    );
};

const ResourceItem: React.FC<{ resource: WebResource }> = ({ resource }) => {
    const { t } = useTranslation();
    return (
        <li className="bg-parchment-50 p-4 rounded-xl border border-[rgba(60,50,40,0.12)] shadow-zen-sm hover:shadow-zen-md transition-all duration-fast ease-zen">
            <div className="flex items-start justify-between gap-2">
                <a
                    href={resource.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-serif font-semibold text-samurai-400 hover:text-samurai-500 hover:underline break-words inline-flex items-start gap-1"
                >
                    {resource.title || resource.url}
                    <ExternalLink className="mt-1 h-3.5 w-3.5 shrink-0" aria-hidden="true" />
                    <span className="sr-only">{t('liveSearch.opensInNewTab')}</span>
                </a>
                {resource.is_free !== null && resource.is_free !== undefined && (
                    <Badge variant={resource.is_free ? 'blue' : 'gold'} className="shrink-0">
                        {resource.is_free ? t('liveSearch.free') : t('liveSearch.paid')}
                    </Badge>
                )}
            </div>
            {resource.description && (
                <p className="text-sm mt-2 text-slate-zen500 line-clamp-3">{resource.description}</p>
            )}
        </li>
    );
};

export default LiveSearchResults;
