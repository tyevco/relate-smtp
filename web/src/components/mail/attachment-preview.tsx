import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Download, Eye, File, X } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import type { EmailAttachment } from '@/api/types'

interface AttachmentPreviewProps {
  emailId: string
  attachment: EmailAttachment
}

function isValidAttachmentId(id: string): boolean {
  // Validate that attachment ID is a valid UUID or safe identifier
  return /^[a-zA-Z0-9-]+$/.test(id)
}

export function AttachmentPreview({ emailId, attachment }: AttachmentPreviewProps) {
  const [isPreviewOpen, setIsPreviewOpen] = useState(false)

  const apiUrl = import.meta.env.VITE_API_URL || '/api'

  // Validate IDs before constructing URL to prevent injection
  const safeEmailId = isValidAttachmentId(emailId) ? emailId : ''
  const safeAttachmentId = isValidAttachmentId(attachment.id) ? attachment.id : ''
  const downloadUrl = safeEmailId && safeAttachmentId
    ? `${apiUrl}/emails/${safeEmailId}/attachments/${safeAttachmentId}`
    : ''

  const isImage = attachment.contentType.startsWith('image/')
  const isPdf = attachment.contentType === 'application/pdf'
  const canPreview = isImage || isPdf

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  const handleDownload = () => {
    if (downloadUrl) {
      window.open(downloadUrl, '_blank')
    }
  }

  return (
    <>
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-3 border rounded-md hover:bg-muted gap-2 sm:gap-0">
        <div className="flex items-center gap-3 flex-1 min-w-0 w-full sm:w-auto">
          <File className="h-5 w-5 text-muted-foreground flex-shrink-0" />
          <div className="flex-1 min-w-0">
            <div className="text-xs sm:text-sm font-medium truncate">{attachment.fileName}</div>
            <div className="text-xs text-muted-foreground">
              {formatFileSize(attachment.sizeBytes)} • {attachment.contentType}
            </div>
          </div>
        </div>

        <div className="flex gap-1 ml-2 w-full sm:w-auto">
          {canPreview && (
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setIsPreviewOpen(true)}
              className="flex-1 sm:flex-none min-h-[44px] text-xs sm:text-sm"
            >
              <Eye className="h-4 w-4 sm:mr-1" />
              <span className="hidden sm:inline">Preview</span>
            </Button>
          )}
          <Button variant="ghost" size="sm" onClick={handleDownload} className="flex-1 sm:flex-none min-h-[44px] text-xs sm:text-sm">
            <Download className="h-4 w-4 sm:mr-1" />
            <span className="hidden sm:inline">Download</span>
          </Button>
        </div>
      </div>

      {/* Preview Dialog */}
      <Dialog open={isPreviewOpen} onOpenChange={setIsPreviewOpen}>
        <DialogContent className="max-w-[95vw] sm:max-w-4xl max-h-[90vh] p-3 sm:p-6">
          <DialogHeader>
            <DialogTitle className="flex items-center justify-between">
              <span className="truncate mr-4 text-sm sm:text-base">{attachment.fileName}</span>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => setIsPreviewOpen(false)}
                className="min-h-[44px]"
              >
                <X className="h-4 w-4" />
              </Button>
            </DialogTitle>
          </DialogHeader>

          <div className="overflow-auto max-h-[calc(90vh-10rem)] sm:max-h-[calc(90vh-8rem)]">
            {isImage && (
              <img
                src={downloadUrl}
                alt={attachment.fileName}
                className="max-w-full h-auto"
              />
            )}

            {isPdf && (
              <iframe
                src={downloadUrl}
                className="w-full h-[50vh] sm:h-[70vh] border-0"
                title={attachment.fileName}
              />
            )}
          </div>

          <div className="flex flex-col sm:flex-row justify-end gap-2 pt-4 border-t">
            <Button variant="outline" onClick={() => setIsPreviewOpen(false)} className="min-h-[44px]">
              Close
            </Button>
            <Button onClick={handleDownload} className="min-h-[44px]">
              <Download className="h-4 w-4 mr-2" />
              Download
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
