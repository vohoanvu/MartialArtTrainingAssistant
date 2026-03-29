// Contact.tsx
import { useTranslation } from 'react-i18next';
import { PageWrapper } from '@/components/zen/PageWrapper';
import { SectionHeader } from '@/components/zen/SectionHeader';

//Unused component
export default function Contact() {
    const { t } = useTranslation();

    return (
        <PageWrapper>
            <div className="max-w-xl mx-auto text-center">
                <SectionHeader title={t("contact.title")} centered />
                <p className="mb-4 font-sans text-ink-400">{t("contact.description")}</p>
                <div className="space-y-4 font-sans text-ink-400">
                    <p>{t("contact.email")} <a href="mailto:vohoanvu96@gmail.com" className="text-samurai-400 hover:text-samurai-500">vohoanvu96@gmail.com</a></p>
                    <p>{t("contact.github")} <a href="https://github.com/vohoanvu" className="text-samurai-400 hover:text-samurai-500">github.com/vohoanvu</a></p>
                </div>
            </div>
        </PageWrapper>
    );
}
